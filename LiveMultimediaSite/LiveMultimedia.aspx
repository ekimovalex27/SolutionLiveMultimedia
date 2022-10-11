<%@ Page Title="" Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" Inherits="LiveMultimedia" Async="true" Codebehind="LiveMultimedia.aspx.cs" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" Runat="Server">

  <style type="text/css">

    .auto-style14 {
      width: 211px;
    }
    #Select1 {
      width: 237px;
    }
    </style>
</asp:Content>

<asp:Content ID="Content2" runat="server" contentplaceholderid="ContentPlaceHolder1">
 
<script src="Scripts/LiveMultimediaScript.js" type="text/javascript"></script>
<div><asp:ScriptManager ID="ScriptManager1" runat="server"></asp:ScriptManager></div>

    <div>
        <asp:UpdatePanel ID="PanelListMultimedia" runat="server">
          <ContentTemplate>
  <div class="LiveMultimediaPlayer">
    <asp:Panel ID="PanelPlayer" runat="server">
    <asp:HiddenField ID="HiddenFieldGUID" runat="server" />

<div class="LiveMultimediaStatus">
    <div id="columns">
        <div id="column1">      
            <div id="row_with_status">                      
                <div class="LiveMultimediaStatus">                           
                    <asp:Label ID="LabelSelectedAlbum" runat="server" CssClass="LiveMultimediaStatus_label" Text="An album is not selected"></asp:Label>              
                </div>            
                <div class="LiveMultimediaStatus">                  
                    <asp:Label ID="LabelSelectedSong" runat="server" CssClass="LiveMultimediaStatus_label" Text="Not selected a song"></asp:Label>              
                </div>
            </div>
            <div id="player">
                <audio id="liveaudio" preload="none" onended="LiveAudio_ended()" onerror="LiveAudio_onerror(this)" controls="controls" src="/music/start.mp3" onvolumechange="LiveAudio_onvolumechange(this)" class="LiveMultimediaPlayer">
                    Your Internet browser don't support Live Multimedia Market
                </audio>
            </div>
        </div>
        <div id="column2">
            <asp:Button ID="cmdBackContentSource" runat="server" CssClass="LiveMultimediaStatus_button" OnClick="cmdBackContentSource_Click" Text="Quick back to the Albums" />
        </div>
    </div>
</div>

    </asp:Panel>
  </div>
  

<div class="LiveMultimediaPlaylist">
            <asp:UpdateProgress ID="UpdateProgressLoadMultimedia" runat="server" AssociatedUpdatePanelID="PanelListMultimedia">
                <ProgressTemplate>
                    <asp:Label ID="LabelLoadingMultimediaStatus" runat="server" Text="Loading your multimedia..." CssClass="UpdateProgressLoadMultimedia"></asp:Label>
                </ProgressTemplate>
            </asp:UpdateProgress>

    <asp:DropDownList ID="ddlPlaylist" runat="server" Height="30px" ToolTip="Play Lists" Width="87px"></asp:DropDownList>
    <asp:TextBox ID="txtNewPlaylist" runat="server" Height="16px" MaxLength="250" Width="69px"></asp:TextBox>
    &nbsp;<asp:RequiredFieldValidator ID="rfvnewPlaylist" runat="server" ControlToValidate="txtNewPlaylist" Display="Dynamic" ForeColor="Red" SetFocusOnError="True">*</asp:RequiredFieldValidator>
    <asp:Button ID="cmdNewPlaylist" runat="server" CausesValidation="False" OnClick="cmdNewPlaylist_Click" Text="New the Playlist" CssClass="LiveMultimediaPlaylistButton" />
    <asp:Button ID="cmdSavePlaylist" runat="server" OnClick="cmdSavePlaylist_Click" Text="Save the Playlist" CssClass="LiveMultimediaPlaylistButton" />
    <asp:Button ID="cmdCancelSavePlaylist" runat="server" CausesValidation="False" OnClick="cmdCancelSavePlaylist_Click" Text="Cancel" CssClass="LiveMultimediaPlaylistButton" />
</div>

            <asp:GridView ID="GridView1" runat="server" AutoGenerateColumns="False" CssClass="PlayButton" GridLines="None" OnRowCommand="GridView1_RowCommand" ShowHeader="False" ShowHeaderWhenEmpty="True" Width="373px"></asp:GridView>
          </ContentTemplate>
        </asp:UpdatePanel>

  </div>

</asp:Content>


