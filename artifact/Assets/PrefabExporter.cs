using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace ImgSpc.Exporters
{
    public class PrefabExporter : MonoBehaviour
    {
        [Tooltip("Filename to export into.")]
        public string DestinationFilename;

        [Tooltip("If false, we always use the ExportMethod below. If true, we'll guess at what export method to use based on the filename extension.")]
        public bool GuessExportMethod = true;

        [Tooltip("Method of export. Must be one of the types in the export method registry.")]
        public string ExportMethod = "obj";

        [Tooltip("Should we create an empty file if there's nothing to export?")]
        public bool ExportEvenIfEmpty = false;

        [Tooltip("Should we open the file explorer to the location of the file after exporting? *Only supported for Windows/Mac/Linux*")]
        public bool OpenInFileExplorer = true;

        [Tooltip("The scale at which to export the mesh(es).")]
        public float ExportSize = 1;

        // Manually assign objects in the Inspector if desired.
        public UnityEngine.Object[] ObjectsToExport;

        // Specify a tag to collect additional objects from the scene.
        [Tooltip("Tag of the objects to also export.")]
        public string ExportTag = "Export";

        // --- Upload settings ---
        [Header("Upload Settings")]
        [Tooltip("URL of the server endpoint to upload the file.")]
        public string uploadURL = "https://yourserver.com/upload"; // Replace with your server URL.
        [Tooltip("API key for authorization when uploading.")]
        public string apiKey = "YOUR_API_KEY"; // Set your API key in the inspector.

        // Keep the original filename separate so we don't keep appending the timestamp.
        private string originalDestinationFilename;

        private void Awake()
        {
            // Save the original filename set in the Inspector. Do this only once.
            originalDestinationFilename = DestinationFilename;
        }

        /// <summary>
        /// Gathers all objects manually assigned in the Inspector and all active objects in the scene with the provided tag,
        /// then exports and uploads them.
        /// </summary>
        public void ExportAndUpload()
        {
            List<UnityEngine.Object> combined = new List<UnityEngine.Object>();

            // Add manually assigned objects.
            if (ObjectsToExport != null && ObjectsToExport.Length > 0)
            {
                combined.AddRange(ObjectsToExport);
            }

            // Add objects found by tag.
            if (!string.IsNullOrEmpty(ExportTag))
            {
                GameObject[] taggedObjects = GameObject.FindGameObjectsWithTag(ExportTag);
                if (taggedObjects != null && taggedObjects.Length > 0)
                {
                    combined.AddRange(taggedObjects);
                }
            }

            // Remove duplicates.
            HashSet<UnityEngine.Object> uniqueObjects = new HashSet<UnityEngine.Object>(combined);
            ObjectsToExport = new UnityEngine.Object[uniqueObjects.Count];
            uniqueObjects.CopyTo(ObjectsToExport);
            Debug.Log($"Combined export list contains {ObjectsToExport.Length} unique objects.");

            StartCoroutine(ExportAndUploadCoroutine());
        }

        private IEnumerator ExportAndUploadCoroutine()
        {
            // Start export.
            Export();
            NetworkedLogger.Instance.Log("Export initiated. Waiting for export to finish...");

            // Wait for export to finish (consider increasing delay or polling for file existence).
            yield return new WaitForSeconds(1f);

            // Determine absolute path.
            string absolutePath = DestinationFilename;
            if (!Path.IsPathRooted(absolutePath))
            {
                absolutePath = Path.Combine(Application.persistentDataPath, DestinationFilename);
            }
            NetworkedLogger.Instance.Log($"Absolute export path for upload: {absolutePath}");

            // Debug: List files in export directory.
            string exportDir = Path.GetDirectoryName(absolutePath);
            string[] files = Directory.GetFiles(exportDir);
            NetworkedLogger.Instance.Log("Files in export directory: " + (files.Length > 0 ? string.Join(", ", files) : "None"));

            // Upload the file.
            yield return StartCoroutine(UploadFile(absolutePath));
        }

        /// <summary>
        /// Uploads the file at the given filePath using UnityWebRequest.
        /// </summary>
        private IEnumerator UploadFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                string directory = Path.GetDirectoryName(filePath);
                string[] files = Directory.GetFiles(directory);
                NetworkedLogger.Instance.Log($"Exported file does not exist: {filePath}. Files in directory ({directory}): " +
                               (files.Length > 0 ? string.Join(", ", files) : "None"));
                yield break;
            }

            byte[] fileData = File.ReadAllBytes(filePath);
            NetworkedLogger.Instance.Log($"Read {fileData.Length} bytes from file: {filePath}");

            WWWForm form = new WWWForm();
            form.AddBinaryData("file", fileData, Path.GetFileName(filePath), "application/octet-stream");
            NetworkedLogger.Instance.Log("File data added to form.");

            using (UnityWebRequest www = UnityWebRequest.Post(uploadURL, form))
            {
                // Set API key header.
                www.SetRequestHeader("x-api-key", apiKey);
                NetworkedLogger.Instance.Log($"Uploading file to: {uploadURL} with API key: {apiKey}");

                yield return www.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
                if (www.result != UnityWebRequest.Result.Success)
#else
                if (www.isNetworkError || www.isHttpError)
#endif
                {
                    NetworkedLogger.Instance.Log("File upload failed: " + www.error + "\nResponse: " + www.downloadHandler.text);
                }
                else
                {
                    NetworkedLogger.Instance.Log("File uploaded successfully. Server response: " + www.downloadHandler.text);
                }
            }
        }

        /// <summary>
        /// Starts the export process using the current fields (DestinationFilename, etc.).
        /// </summary>
        public void Export()
        {
            if (ObjectsToExport == null || ObjectsToExport.Length == 0)
            {
                NetworkedLogger.Instance.Log("No objects to export. Please ensure 'ObjectsToExport' is populated.");
                return;
            }

            // Generate a timestamped filename.
            string baseFilename = Path.GetFileNameWithoutExtension(originalDestinationFilename);
            string extension = Path.GetExtension(originalDestinationFilename);
            string dateStamp = DateTime.Now.ToString("yyyyMMdd'T'HHmmss");
            string timestampedFilename = $"{baseFilename}_{dateStamp}{extension}";
            NetworkedLogger.Instance.Log($"Timestamped filename generated: {timestampedFilename}");

            // Determine absolute path.
            string absolutePath = timestampedFilename;
            if (!Path.IsPathRooted(absolutePath))
            {
                absolutePath = Path.Combine(Application.persistentDataPath, timestampedFilename);
            }
            NetworkedLogger.Instance.Log($"Absolute export path for export: {absolutePath}");

            // Update the DestinationFilename for later use.
            DestinationFilename = timestampedFilename;

            // Determine export method.
            ExportMethod method = null;
            if (GuessExportMethod)
            {
                method = ExportMethodRegistry.GuessMethodByExtension(absolutePath);
                NetworkedLogger.Instance.Log($"Guessed export method: {(method != null ? method.Extension : "none")}");
            }
            if (method == null)
            {
                method = ExportMethodRegistry.GetMethod(ExportMethod);
                NetworkedLogger.Instance.Log($"Fallback export method: {(method != null ? method.Extension : "none")}");
            }
            if (method == null)
            {
                NetworkedLogger.Instance.Log("ImgSpcExport: Unknown export method " + Path.GetExtension(absolutePath));
                return;
            }

            // Skip export if there is no valid mesh data.
            if (!ExportEvenIfEmpty)
            {
                if (TestIfExportIsEmpty("ImgSpcExport", ObjectsToExport))
                {
                    NetworkedLogger.Instance.Log("Export canceled because there is no mesh data to export.");
                    return;
                }
            }

            // Ensure correct file extension.
            string currentExtension = Path.GetExtension(absolutePath).TrimStart('.').ToLower();
            if (!currentExtension.Equals(method.Extension))
            {
                absolutePath += "." + method.Extension;
                NetworkedLogger.Instance.Log($"File extension adjusted to match export method. New path: {absolutePath}");
            }
            NetworkedLogger.Instance.Log("Final Destination Filename: " + absolutePath);

            // Instantiate exporter and export all objects.
            using (var exporter = method.Instantiate(absolutePath, ExportSize))
            {
                int exportedCount = ExportAll(exporter, ObjectsToExport, warn: false);
                NetworkedLogger.Instance.Log($"Exported {exportedCount} objects using method {method.Extension}");
            }

            // Immediately check if the file now exists.
            if (File.Exists(absolutePath))
            {
                NetworkedLogger.Instance.Log("Export completed successfully and file exists: " + absolutePath);
            }
            else
            {
                NetworkedLogger.Instance.Log("Export completed but file does not exist at: " + absolutePath);
            }

#if UNITY_STANDALONE || UNITY_EDITOR
            if (OpenInFileExplorer)
            {
                OpenInFileBrowser.Open(absolutePath);
            }
#endif
        }

        /// <summary>
        /// Sets the filename. This should be a full path.
        /// </summary>
        public void SetFilename(string name)
        {
            DestinationFilename = name;
            originalDestinationFilename = name;
            NetworkedLogger.Instance.Log("Destination filename set to: " + DestinationFilename);
        }

        /// <summary>
        /// Checks if the export is empty. If it is, logs a warning and returns true.
        /// </summary>
        public static bool TestIfExportIsEmpty(string dialogTitle, IEnumerable<UnityEngine.Object> objectsToExport)
        {
            using (var counter = new CountingExporter())
            {
                int n = ExportAll(counter, objectsToExport);
                NetworkedLogger.Instance.Log($"{dialogTitle}: {counter.NumMeshes} meshes and {counter.NumTriangles} triangles found.");
                if (counter.NumTriangles == 0)
                {
                    switch (counter.NumMeshes)
                    {
                        case 0:
                            Debug.LogWarning(n == 0 ? $"{dialogTitle}: There is nothing to export." : $"{dialogTitle}: There are objects to export but none has a mesh.");
                            break;
                        case 1:
                            Debug.LogWarning($"{dialogTitle}: The selected mesh is empty.");
                            break;
                        default:
                            Debug.LogWarning($"{dialogTitle}: The selected meshes are all empty.");
                            break;
                    }
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Export all the objects in the set, returning the number of objects exported.
        /// </summary>
        public static int ExportAll(AbstractMeshExporter exporter, IEnumerable<UnityEngine.Object> exportSet, bool warn = true)
        {
            int n = 0;
            foreach (var obj in exportSet)
            {
                ++n;
                if (obj is Transform xform)
                {
                    exporter.Export(xform.gameObject);
                }
                else if (obj is GameObject go)
                {
                    exporter.Export(go);
                }
                else if (obj is MonoBehaviour mono)
                {
                    exporter.Export(mono.gameObject);
                }
                else if (obj is Mesh mesh)
                {
                    exporter.ExportMesh(new AbstractMeshExporter.MeshInfo(mesh));
                }
                else
                {
                    if (warn)
                    {
                        NetworkedLogger.Instance.Log(obj != null
                            ? "ImgSpcExport: Not exporting object of type " + obj.GetType() + " (" + obj.name + ")"
                            : "ImgSpcExport: Not exporting null object");
                    }
                    --n;
                }
            }
            return n;
        }
    }
}
