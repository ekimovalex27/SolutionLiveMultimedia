namespace LiveMultimediaOAuth
{
  using System;

  public static class OAuthConstants
  {
    #region OAuth 2.0 standard parameters
    public const string ClientID = "client_id";
    public const string ClientSecret = "client_secret";
    public const string Callback = "redirect_uri";
    public const string ClientState = "state";
    public const string Scope = "scope";
    public const string Code = "code";
    public const string AccessToken = "access_token";
    public const string AuthenticationToken = "authentication_token";
    public const string ExpiresIn = "expires_in";
    public const string RefreshToken = "refresh_token";
    public const string ResponseType = "response_type";
    public const string GrantType = "grant_type";
    public const string Error = "error";
    public const string ErrorDescription = "error_description";
    public const string Display = "display";
    #endregion
  }

  public static class OAuthConstantsObjects
    {
      #region OAuth 2.0 objects standard parameters
      public const string Data = "data";
      public const string Id = "id";
      public const string From = "from";
      public const string NameFromObject = "name (from object)";
      public const string IdFromObject = "id (from object)";
      public const string Name = "name";
      public const string Description = "description";
      public const string Count = "count";
      public const string Link = "link";
      public const string ParentId = "parent_id";
      public const string UploadLocation = "upload_location";
      public const string IsEmbeddable = "is_embeddable";
      public const string Type = "type";
      public const string CreatedTime = "created_time";
      public const string UpdatedTime = "updated_time";
      public const string SharedWith = "shared_with";
      public const string AccessShared = "access (shared_with object)";
      public const string Size = "size";
      public const string CommentsCount="comments_count";
      public const string CommentsEnabled="comments_enabled";
      public const string Source = "source";
      public const string Title="title";
      public const string Artist = "artist";
      public const string Album = "album";
      public const string AlbumArtist = "album_artist";
      public const string Genre = "genre";
      public const string Duration = "duration";
      public const string Picture = "picture";
      #endregion OAuth 2.0 objects standard parameters
    }

}