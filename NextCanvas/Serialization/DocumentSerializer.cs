﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NextCanvas.Interactivity.Progress;
using NextCanvas.Models;
using NextCanvas.Models.Content;
using ErrorEventArgs = Newtonsoft.Json.Serialization.ErrorEventArgs;

namespace NextCanvas.Serialization
{
    public class DocumentSerializer
    {
        private readonly JsonSerializerSettings serializerSettings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto,
            Error = IgnoreError
        };

        private static void IgnoreError(object sender, ErrorEventArgs e)
        {
            if (e.CurrentObject is Page p &&
                Regex.IsMatch(e.ErrorContext.Path, @"Pages\.\$values\[[0-9]+\]\.Elements\.\$type")) // If type is wrong
            {
                p.Elements.Add(new ContentElement()); // oh no
            }
            e.ErrorContext.Handled = true;
        }

        // TODO: Implement smart zip updating.
        public Task SaveCompressedDocument(Document document, string savePath, IProgressInteraction progress = null)
        {
            return CreateZipFile(document, savePath, progress);
        }

        private void CreateZipFile(Document document, string savePath)
        {
            using (var zip = new ZipFile(Encoding.UTF8))
            {
                AddDocumentJson(document, zip);
                zip.AddDirectoryByName("resources");
                foreach (Resource resource in document.Resources)
                {
                    resource.Data.Position = 0;
                    zip.AddEntry($"resources\\{resource.Name}", resource.Data);
                }

                zip.Save(savePath);
            }
        }

        private void AddDocumentJson(Document document, ZipFile zip)
        {
            var mainJson = JsonConvert.SerializeObject(document, serializerSettings);
            zip.AddEntry("document.json", mainJson, Encoding.UTF8); // Fixes the things
        }

        private async Task CreateZipFile(Document document, string savePath, IProgressInteraction progress)
        {
            using (var zip = new ZipFile())
            {
                ProgressTask writingTask = CreateTasksInitialization(
                    document,
                    progress,
                    out List<ProgressTask> resourceTasks,
                    out int count,
                    out ProgressTask finalizingTask);
                await progress.Show();
                writingTask.Progress = 25;
                AddDocumentJson(document, zip);
                zip.AddDirectoryByName("resources");
                writingTask.Complete();
                if (count == 0)
                {
                    FinalizeFileTask(savePath, finalizingTask, zip);
                    return;
                }
                for (var index = 0; index < document.Resources.Count; index++)
                {
                    ProgressTask task = resourceTasks[index];
                    if (index == 0)
                    {
                        zip.AddProgress += (sender, args) =>
                                           {
                                               if (args.BytesTransferred == 0 || args.TotalBytesToTransfer == 0)
                                               {
                                                   return;
                                               }
                                               double percentage = args.BytesTransferred +
                                                                   0.01 / args.TotalBytesToTransfer +
                                                                   0.01;
                                               string percentageString = percentage.ToString("P");
                                               task.Progress = percentage * 100;
                                               task.ProgressText = percentageString;
                                               Debug.WriteLine($"percentage : {percentageString}");
                                           };
                    }
                    Resource resource = document.Resources[index];

                    task.Progress = 50;
                    resource.Data.Position = 0;
                    zip.AddEntry($"resources\\{resource.Name}", resource.Data);
                    await Task.Delay(750);
                    resourceTasks[index].Complete();
                }
                FinalizeFileTask(savePath, finalizingTask, zip);
            }
        }

        private static ProgressTask CreateTasksInitialization(
            Document document,
            IProgressInteraction progress,
            out List<ProgressTask> resourceTasks,
            out int count,
            out ProgressTask finalizingTask)
        {
            var writingTask = new ProgressTask(10, "Writing document base data...");
            List<ProgressTask> tasks = new List<ProgressTask>
            {
                writingTask
            };
            resourceTasks = new List<ProgressTask>();
            count = document.Resources.Count;
            for (var i = 0; i < count; i++)
            {
                var progressTask = new ProgressTask(80, $"Writing resource : {document.Resources[i].Name}...");
                tasks.Add(progressTask);
                resourceTasks.Add(progressTask);
            }

            finalizingTask = new ProgressTask(5, "Saving to file...");
            tasks.Add(finalizingTask);
            TaskManager<IProgressInteraction> taskManager = new TaskManager<IProgressInteraction>(progress, tasks);
            return writingTask;
        }

        private static void FinalizeFileTask(string savePath, ProgressTask finalizingTask, ZipFile zip)
        {
            finalizingTask.Progress = 50;
            FinalizeFile(savePath, zip);
            finalizingTask.Complete();
        }

        private static void FinalizeFile(string savePath, ZipFile zip)
        {
            zip.Save(savePath);
        }

        public Document TryOpenDocument(FileStream fileStream)
        {
            try
            {
                return OpenCompressedFileFormat(fileStream);
            }
            catch (ZipException) // Try reading as json
            {
                return OpenJson(fileStream);
            }
        }

        public Document OpenCompressedFileFormat(Stream fileStream)
        {
            using (var zipFile = ZipFile.Read(fileStream))
            {
                Document doc = GetDocumentJson(zipFile);
                foreach (Resource resource in doc.Resources)
                {
                    ProcessDataCopying(zipFile, resource); // Copy the deeta to the resources.
                }
                // AttachResources(doc); // Attach them to all the elements.
                return doc; // Yeah we're done :) dope nah?
            }
        }

        private Document GetDocumentJson(ZipFile zipFile)
        {
            var documentJson = zipFile.Entries.First(e => e.FileName == "document.json");
            Document doc;
            using (var docReader = documentJson.OpenReader())
            {
                doc = GetBaseDocumentJson(docReader);
            }

            return doc;
        }

        private Document GetBaseDocumentJson(Stream docReader)
        {
            Document doc;
            using (var streamReader = new StreamReader(docReader))
            {
                doc = ReadDocumentJson(streamReader);
            }

            return doc;
        }

        private static void ProcessDataCopying(ZipFile zipFile, Resource resource)
        {
            var data =
                zipFile.Entries.First(
                    e => e.FileName.Equals($"resources/{resource.Name}", StringComparison.InvariantCultureIgnoreCase));
            var stream = new MemoryStream();
            data.Extract(stream);
            resource.Data = stream;
        }

        public Document OpenJson(Stream fileStream)
        {
            using (var streamyStream = new StreamReader(fileStream))
            {
                return ReadDocumentJson(streamyStream);
            }
        }

        private Document ReadDocumentJson(TextReader streamyStream)
        {
            string value = streamyStream.ReadToEnd();
            var deserialized = JsonConvert.DeserializeObject<Document>(value, serializerSettings);
            return deserialized;
        }
    }
}