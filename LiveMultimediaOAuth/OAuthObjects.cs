namespace LiveMultimediaOAuth
{
  using System;
  using System.Runtime.Serialization;
  using System.Runtime.Serialization.Json;

  [DataContract]
  public class OAuthError
  {
    #region OAuthError

    [DataMember(Name = OAuthConstants.Error)]
    public string Code { get; set; }

    [DataMember(Name = OAuthConstants.ErrorDescription)]
    public string Description { get; set; }

    #endregion OAuthError
  }

  [DataContract]
  public class OAuthToken
  {
    #region OAuthObjectToken

    [DataMember(Name = OAuthConstants.AccessToken)]
    public string AccessToken { get; set; }

    [DataMember(Name = OAuthConstants.AuthenticationToken)]
    public string AuthenticationToken { get; set; }

    [DataMember(Name = OAuthConstants.RefreshToken)]
    public string RefreshToken { get; set; }

    [DataMember(Name = OAuthConstants.ExpiresIn)]
    public string ExpiresIn { get; set; }

    [DataMember(Name = OAuthConstants.Scope)]
    public string Scope { get; set; }

    #endregion OAuthObjectToken
  }

  [DataContract]
  public class OAuthObjectFolder
  {
    #region OAuthObjectFolder

    [DataMember(Name = OAuthConstantsObjects.Data)]
    public object[] Data { get; set; }

    [DataMember(Name = OAuthConstantsObjects.Id)]
    public string Id { get; set; }

    [DataMember(Name = OAuthConstantsObjects.From)]
    public Object From { get; set; }

    [DataMember(Name = OAuthConstantsObjects.NameFromObject)]
    public string NameFromObject { get; set; }

    [DataMember(Name = OAuthConstantsObjects.IdFromObject)]
    public string IdFromObject { get; set; }

    [DataMember(Name = OAuthConstantsObjects.Name)]
    public string Name { get; set; }

    [DataMember(Name = OAuthConstantsObjects.Description)]
    public string Description { get; set; }

    [DataMember(Name = OAuthConstantsObjects.Count)]
    public int Count { get; set; }

    [DataMember(Name = OAuthConstantsObjects.Link)]
    public string Link { get; set; }

    [DataMember(Name = OAuthConstantsObjects.ParentId)]
    public string ParentId { get; set; }

    [DataMember(Name = OAuthConstantsObjects.UploadLocation)]
    public string UploadLocation { get; set; }

    [DataMember(Name = OAuthConstantsObjects.IsEmbeddable)]
    public bool IsEmbeddable { get; set; }

    [DataMember(Name = OAuthConstantsObjects.Type)]
    public string Type { get; set; }

    [DataMember(Name = OAuthConstantsObjects.CreatedTime)]
    public string CreatedTime { get; set; }

    [DataMember(Name = OAuthConstantsObjects.UpdatedTime)]
    public string UpdatedTime { get; set; }

    [DataMember(Name = OAuthConstantsObjects.SharedWith)]
    public Object SharedWith { get; set; }

    [DataMember(Name = OAuthConstantsObjects.AccessShared)]
    public string AccessShared { get; set; }

    #endregion OAuthObjectFolder
  }

  [DataContract]
  public class OAuthObjectAudio
  {
    #region OAuthObjectAudio

    [DataMember(Name = OAuthConstantsObjects.Data)]
    public object[] Data { get; set; }

    [DataMember(Name = OAuthConstantsObjects.Id)]
    public string Id { get; set; }

    [DataMember(Name = OAuthConstantsObjects.From)]
    public Object From { get; set; }

    [DataMember(Name = OAuthConstantsObjects.NameFromObject)]
    public string NameFromObject { get; set; }

    [DataMember(Name = OAuthConstantsObjects.IdFromObject)]
    public string IdFromObject { get; set; }

    [DataMember(Name = OAuthConstantsObjects.Name)]
    public string Name { get; set; }

    [DataMember(Name = OAuthConstantsObjects.Description)]
    public string Description { get; set; }

    [DataMember(Name = OAuthConstantsObjects.ParentId)]
    public string ParentId { get; set; }

    [DataMember(Name = OAuthConstantsObjects.Size)]
    public int Size { get; set; }

    [DataMember(Name = OAuthConstantsObjects.UploadLocation)]
    public string UploadLocation { get; set; }

    [DataMember(Name = OAuthConstantsObjects.CommentsCount)]
    public int CommentsCount { get; set; }

    [DataMember(Name = OAuthConstantsObjects.CommentsEnabled)]
    public bool CommentsEnabled { get; set; }

    [DataMember(Name = OAuthConstantsObjects.IsEmbeddable)]
    public bool IsEmbeddable { get; set; }

    [DataMember(Name = OAuthConstantsObjects.Source)]
    public string Source { get; set; }

    [DataMember(Name = OAuthConstantsObjects.Link)]
    public string Link { get; set; }

    [DataMember(Name = OAuthConstantsObjects.Type)]
    public string Type { get; set; }

    [DataMember(Name = OAuthConstantsObjects.Title)]
    public string Title { get; set; }

    [DataMember(Name = OAuthConstantsObjects.Artist)]
    public string Artist { get; set; }

    [DataMember(Name = OAuthConstantsObjects.Album)]
    public string Album { get; set; }

    [DataMember(Name = OAuthConstantsObjects.AlbumArtist)]
    public string AlbumArtist { get; set; }

    [DataMember(Name = OAuthConstantsObjects.Genre)]
    public string Genre { get; set; }

    [DataMember(Name = OAuthConstantsObjects.Duration)]
    public int Duration { get; set; }

    [DataMember(Name = OAuthConstantsObjects.Picture)]
    public string Picture { get; set; }

    [DataMember(Name = OAuthConstantsObjects.SharedWith)]
    public Object SharedWith { get; set; }

    [DataMember(Name = OAuthConstantsObjects.AccessShared)]
    public string AccessShared { get; set; }

    [DataMember(Name = OAuthConstantsObjects.CreatedTime)]
    public string CreatedTime { get; set; }

    [DataMember(Name = OAuthConstantsObjects.UpdatedTime)]
    public string UpdatedTime { get; set; }

    #endregion OAuthObjectAudio
  }

  [DataContract]
  public class OAuthSourceMultimedia
  {
    #region OAuthSourceMultimedia

    [DataMember]
    public string OAuthUrlSignIn { get; set; }

    [DataMember]
    public string OAuthUrlSignOut { get; set; }

    [DataMember]
    public string OAuthUrlToken { get; set; }
    [DataMember]
    public string OAuthUrlRefreshToken { get; set; }

    #endregion OAuthSourceMultimedia
  }

}
