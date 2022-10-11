using System.Collections.Generic;
using System.Threading.Tasks;

namespace LiveMultimediaREST
{
  public interface IREST
  {
    List<LiveMultimediaOAuth.OAuthObjectFolder> GetListMultimediaFolderGoogleDrive();

    List<LiveMultimediaOAuth.OAuthObjectFolder> GetListMultimediaFolderOneDrive();

    List<LiveMultimediaOAuth.OAuthObjectFolder> GetListMultimediaFolderVKontakte();

    List<LiveMultimediaOAuth.OAuthObjectAudio> GetListMultimediaItem(string[] ListId);

    List<LiveMultimediaOAuth.OAuthObjectAudio> GetListMultimediaItemGoogleDrive(string[] ListId);

    List<LiveMultimediaOAuth.OAuthObjectAudio> GetListMultimediaItemVKontakte(string[] ListIdFolder);

    Task<string> GetMultimediaItemSourceGoogleDrive(string IdObjectAudio);

    Task<long> GetMultimediaItemSourceOneDrive(string IdItem);

    Task<string> GetDownloadURLVKontakte(string IdItem);
  }
}
