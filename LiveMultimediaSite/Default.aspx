<%@ Page Title="" Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="LiveMultimediaSite.Default" Async="true" %>
<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">   

  <asp:ScriptManager ID="ScriptManager1" runat="server"></asp:ScriptManager>

<script src="Scripts/LiveMultimediaScript.js" type="text/javascript"></script>

<div id="breadCrumpsDiv" class="BreadCrumpsDiv">
  <asp:PlaceHolder ID="PlaceBreadCrumbs" runat="server"></asp:PlaceHolder>
  <hr class="BreadCrumpsDelimiter"/>
</div>

<asp:UpdatePanel ID="PanelListMultimedia" runat="server">        
  <ContentTemplate>
    <asp:Table ID="TableItem" CssClass="TableSourceItem" runat="server"></asp:Table>
  </ContentTemplate>
</asp:UpdatePanel>

<div id="LiveMultimedia" hidden="hidden" style="display:none">

  <div id="spinner" class="spinner">
    <div class="rect1"></div>
    <div class="rect2"></div>
    <div class="rect3"></div>
    <div class="rect4"></div>
    <div class="rect5"></div>
  </div>

  <div id="AudioButtons" class="AudioButtons">                    
    <div id="AudioButtonPause" class="AudioButtonPause" onclick="AudioButtonPause_click()">
      <div></div>
      <div></div>
    </div>
    <div id="AudioButtonPlay" class="AudioButtonPlay" onclick="AudioButtonPlay_click()"></div>
    <div id="AudioButtonLoop" class="AudioButtonLoop" onclick="AudioButtonLoop_click()"></div>
  </div>

  <audio id="liveaudio" preload="none" controls=""
    onemptied="LiveAudio_emptied()"
    onwaiting="LiveAudio_waiting()"
    onloadstart="LiveAudio_loadstart()"
    onloadeddata="LiveAudio_loadeddata()"
    onplaying="LiveAudio_playing()"
    onended="LiveAudio_ended()"
    onstalled="LiveAudio_stalled()"    
    onerror="LiveAudio_onerror()" 
    >
    Your Internet browser don't support Live Multimedia Market
  </audio>

  <div id="videoButtons">    
    <video id="livevideo" class="LiveVideoPlayer" preload="none" autoplay="autoplay" controls="controls"
      onemptied="LiveVideo_emptied()"
      onwaiting="LiveVideo_waiting()"
      onloadstart="LiveVideo_loadstart()"
      onloadeddata="LiveVideo_loadeddata()"
      onplaying="LiveVideo_playing()"
      onended="LiveVideo_ended()"
      onstalled="LiveVideo_stalled()"
      onerror="LiveVideo_onerror()">
      Your Internet browser don't support Live Multimedia Market
    </video>
    <div id="closeVideo" class="closeVideo" title="Close">X</div>
  </div>

  <img id="liveimage" src="Images/spinner.gif" alt="Please wait" class="Spinner2" hidden="hidden" style="display:none"/>

  <iframe id="liveframe" class="LivePicturePlayer"></iframe>
</div>

</asp:Content>
