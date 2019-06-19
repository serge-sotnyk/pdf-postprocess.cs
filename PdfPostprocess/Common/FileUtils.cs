using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PdfPostprocess.Common
{
    public static class FileUtils
    {
        /// <summary>
        /// Method returns absolute path to the first folder with 
        /// passed name in one of the root folders.
        /// </summary>
        /// <param name="folderName">Folder to search.</param>
        /// <param name="startFolder">Start folder. Pass null to use currect folder as start.</param>
        /// <returns>Absolute path to found folder.</returns>
        public static string FindFolderInRoots(string folderName, string startFolder = null)
        {
            if (startFolder == null)
                startFolder = Environment.CurrentDirectory;
            var currentFolder = new DirectoryInfo(startFolder);
            do
            {
                var pathToCheck = Path.Combine(currentFolder.FullName, folderName);
                if (Directory.Exists(pathToCheck))
                    return pathToCheck;
                currentFolder = currentFolder.Parent;
            } while (currentFolder != null);
            throw new DirectoryNotFoundException($"Folder '{folderName}' not found in every level of path '{startFolder}'");
        }
    }
}
