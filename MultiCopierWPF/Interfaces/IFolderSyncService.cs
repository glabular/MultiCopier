using System.IO;

namespace MultiCopierWPF.Interfaces
{
    public interface IFolderSyncService
    {
        /// <summary>
        /// Recursively synchronizes the specified source directory with the target directory.
        /// This operation copies new and updated files and directories from source to target,
        /// and deletes files and directories in the target that no longer exist in the source,
        /// resulting in the target being an exact mirror of the source.
        /// </summary>
        /// <param name="source">The source directory to mirror from (master folder).</param>
        /// <param name="target">The target directory to mirror to (backup location).</param>
        void Mirror(DirectoryInfo source, DirectoryInfo target);
    }
}
