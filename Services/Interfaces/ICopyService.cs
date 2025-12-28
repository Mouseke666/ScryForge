using ScryForge.Models;

namespace ScryForge.Services
{
    public interface ICopyService
    {
        void CopyFilesToRoot(string path);

        void DuplicateCards(List<CardInfo> cards);

        void CopyFolderFiles(string sourceFolder, string destinationFolder, bool overwrite = true);

        void MoveFile(string sourceFile, string destinationFile, bool overwrite = true);

        void CopyFile(string sourceFile, string destinationFile, bool overwrite = true);
    }
}
