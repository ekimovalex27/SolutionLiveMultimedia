var PanelListMultimedia;
var selecteditem;
var PressedItem = null;
var MultimediaItem;
var title;
var OriginalbackgroundColor = "";
var NonPlaybackgroundColor = "rgb(128, 128, 128)";
var ErrorPlaybackgroundColor = "rgb(0, 0, 0)";

var SelectedRow = 0, SelectedColumn = -1;
var spinner;
var isCloseVideo = false;

function generateUUID() {
    var d = new Date().getTime();
    var uuid = 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
        var r = (d + Math.random() * 16) % 16 | 0;
        d = Math.floor(d / 16);
        return (c == 'x' ? r : (r & 0x3 | 0x8)).toString(16);
    });
    return uuid;
};

function LiveAudio_play() {
    var item;    
    var PlayingSong;

    PanelListMultimedia = document.getElementById("ContentPlaceHolder1_PanelListMultimedia");

    var MaxCountRows = PanelListMultimedia.childNodes[1].rows.length;
    var MaxCountColumns = PanelListMultimedia.childNodes[1].rows[0].cells.length;
    var MaxCountColumnsInRow;

    if (PressedItem == null) {
      var NewSelectedCountRow = -1;
      var NewSelectedCountColumn = -1;
      var isFoundMultimediaItem = false;
      var isExistsNotPlayedItems = true;

      do {
        for (var CountRows = SelectedRow; CountRows < MaxCountRows; CountRows++) {
          MaxCountColumnsInRow = PanelListMultimedia.childNodes[1].rows[CountRows].cells.length;
          for (var CountColumns = SelectedColumn + 1; CountColumns < MaxCountColumnsInRow; CountColumns++) {
            item = PanelListMultimedia.childNodes[1].rows[CountRows].cells[CountColumns].childNodes[0];
            if (item != null) {
              var TypeMultimediaItem = item.attributes["TypeMultimediaItem"].value;
              if (TypeMultimediaItem!=2) {
                if (item.attributes["IsPlayed"].value == "0" && item.attributes["IsError"].value == "0") {
                  if (OriginalbackgroundColor == "") OriginalbackgroundColor = item.style.backgroundColor;
                  selecteditem = item;
                  isFoundMultimediaItem = true;

                  NewSelectedCountColumn = CountColumns; NewSelectedCountRow = CountRows;
                  //CountColumns = MaxCountColumns; Закомментировал после публикации рабочей версии. Всё работает без этого? 25.09.2015 12:33
                  break;
                }
                else isExistsNotPlayedItems = false;
              }
            }
          }
          if (isFoundMultimediaItem) break;
          SelectedColumn = -1;
        }
        SelectedRow = 0; SelectedColumn = -1;
      } while (isExistsNotPlayedItems && !isFoundMultimediaItem);

      if (isFoundMultimediaItem) {
        SelectedColumn = NewSelectedCountColumn;
        SelectedRow = NewSelectedCountRow;
      }
    }
    else {
      isFoundMultimediaItem = false;
      selecteditem = null;

      for (var CountRows = 0; CountRows < MaxCountRows; CountRows++) {
        MaxCountColumnsInRow = PanelListMultimedia.childNodes[1].rows[CountRows].cells.length;
        for (var CountColumns = 0; CountColumns < MaxCountColumnsInRow; CountColumns++) {
          item = PanelListMultimedia.childNodes[1].rows[CountRows].cells[CountColumns].childNodes[0];
          if (item != null) {
            if (PressedItem == item) {
              if (OriginalbackgroundColor == "") OriginalbackgroundColor = item.style.backgroundColor;
              SelectedRow = CountRows; SelectedColumn = CountColumns;
              isFoundMultimediaItem = true;
              selecteditem = PressedItem;
              break;
            }
          }
        }
      }      
    }

    if (isFoundMultimediaItem) {
      if (PressedItem == null) {        
        selecteditem.scrollIntoView(true);
      }
      else {
        PressedItem = null;
      }

      SetStateControl(true);

      src = selecteditem.attributes["Url"].value + "&" + "UMR=" + generateUUID();

      selecteditem.style.backgroundColor = OriginalbackgroundColor;

      if (selecteditem.attributes["IsError"].value == "1") {
        selecteditem.firstChild.textContent = selecteditem.attributes["savedText"].value;
        selecteditem.attributes["IsError"].value = "0";
      }

      spinner = document.getElementById("spinner");
      spinner.style.marginTop = "-" + selecteditem.parentElement.style.height;
      spinner.style.height = selecteditem.clientHeight / 1.5;
      selecteditem.parentElement.appendChild(spinner);

      var TypeMultimediaItem = selecteditem.attributes["TypeMultimediaItem"].value;

      if (TypeMultimediaItem == "3") {        
        MultimediaItem = document.getElementById("liveaudio");
        if (!MultimediaItem.paused) MultimediaItem.pause();

        MultimediaItem.src = src;        
        MultimediaItem.load();
        MultimediaItem.play();
          
        //selecteditem.attributes["IsPlayed"].value = "1";
      }

      if (TypeMultimediaItem == "4") {
        isCloseVideo = false;

        MultimediaItem = document.getElementById("livevideo");
        //if (!MultimediaItem.paused) MultimediaItem.pause();

        MultimediaItem.src = src;        
        MultimediaItem.load();
        MultimediaItem.play();

        //Для отладки
        //LiveVideo_playing();

        //selecteditem.attributes["IsPlayed"].value = "1";
      }

      if (TypeMultimediaItem == "5") {
        MultimediaItem = document.getElementById("liveimage");
        MultimediaItem.className = "PlayerImage";
        MultimediaItem.hidden = "";
        MultimediaItem.style.display = "block";
        MultimediaItem.src = src;
      }

      SetStateControl(false);
    }
    else {
        SetStateControl(true);
    }
}

function LiveAudio_Play_Next() {
  if (MultimediaItem) MultimediaItem.pause();
  LiveAudio_play();

  // для отладки
  //var PanelListMultimedia = document.getElementById("ContentPlaceHolder1_PanelListMultimedia");
  //selecteditem = PanelListMultimedia.childNodes[1].rows[0].cells[0].childNodes[0];
  //MultimediaItem = document.getElementById("liveaudio");
  //LiveAudio_loadeddata();
}

/* Occurs when the media element is reset to its initial state. */
function LiveAudio_emptied() {
  spinner = document.getElementById("spinner");
  spinner.hidden = "";
  spinner.style.display = "table-cell";

  audioButtons = document.getElementById("AudioButtons");
  audioButtons.hidden = "hidden";
  audioButtons.style.display = "none";
}

/* Occurs when playback stops because the next frame of a video resource is not available. */
function LiveAudio_waiting() {
}

/* Occurs when Internet Explorer begins looking for media data. */
function LiveAudio_loadstart() {
}

/* Occurs when media data is loaded at the current playback position. */
function LiveAudio_loadeddata() {
}

/* Occurs when the audio or video has started playing. */
function LiveAudio_playing() {
  selecteditem.attributes["IsPlayed"].value = "1";
  selecteditem.style.backgroundColor = NonPlaybackgroundColor;

  elementHide("spinner");

  if (selecteditem.attributes["IsError"].value != "1") {
    var parentWidth = parseInt(selecteditem.parentElement.style.width != "" ? selecteditem.parentElement.style.width : selecteditem.parentElement.clientWidth, 10);
    var parentHeight = parseInt(selecteditem.parentElement.style.height, 10);
    var maximumHeight = parentWidth / 4;

    var halfHeight = parentHeight * 3 / 4;
    if (halfHeight > maximumHeight) halfHeight = maximumHeight;

    var quarterHeight = halfHeight / 2;
    var marginLeft = ((parentWidth - (maximumHeight * 3)) / 2) - 5;

    //for debug
    //selecteditem.firstChild.textContent = "parentWidth=" + parentWidth + ", parentHeight=" + parentHeight + ", maximumHeight=" + maximumHeight + ", halfHeight=" + halfHeight;

    MultimediaItem.loop = false;

    var audioButtons = document.getElementById("AudioButtons");
    audioButtons.style.marginTop = "-" + (halfHeight + 5) + "px";

    /* audioButtonPause */
    var audioButtonPause = document.getElementById("AudioButtonPause");
    audioButtonPause.style.width = halfHeight + "px";
    audioButtonPause.style.height = halfHeight + "px";
    audioButtonPause.style.fontSize = halfHeight + "px";

    /* audioButtonPlay */
    var audioButtonPlay = document.getElementById("AudioButtonPlay");
    audioButtonPlay.style.borderTopWidth = quarterHeight + "px";
    audioButtonPlay.style.borderLeftWidth = halfHeight + "px";
    audioButtonPlay.style.borderBottomWidth = quarterHeight + "px";
    audioButtonPlay.style.marginLeft = marginLeft + "px";

    /* audioButtonLoop */
    var audioButtonLoop = document.getElementById("AudioButtonLoop");
    audioButtonLoop.style.width = halfHeight + "px";
    audioButtonLoop.style.height = halfHeight + "px";
    audioButtonLoop.style.borderWidth = (halfHeight / 25) + "px";
    audioButtonLoop.style.marginLeft = marginLeft + "px";
    audioButtonLoop.style.marginTop = "-" + (parseInt(audioButtonLoop.style.borderWidth, 10) + 5) + "px";

    borderLeftWidth = (halfHeight / 7);
    borderRightWidth = borderLeftWidth;

    var styleBefore = document.createElement('style');
    styleBefore.innerHTML = '.AudioButtonLoop::before{';
    styleBefore.innerHTML += 'border-left-width: ' + borderLeftWidth + 'px;'
    styleBefore.innerHTML += 'border-right-width: ' + borderRightWidth + 'px;'
    styleBefore.innerHTML += 'border-bottom-width: ' + borderLeftWidth * 2 + 'px;';
    styleBefore.innerHTML += 'top: ' + borderLeftWidth * 2 + 'px;';
    styleBefore.innerHTML += 'left: ' + '-' + borderLeftWidth + 'px;';
    styleBefore.innerHTML += '}';
    document.querySelector('.AudioButtonLoop').appendChild(styleBefore);

    var styleAfter = document.createElement('style');
    styleAfter.innerHTML = '.AudioButtonLoop::after{';
    styleAfter.innerHTML += 'border-left-width: ' + borderLeftWidth + 'px;'
    styleAfter.innerHTML += 'border-right-width: ' + borderRightWidth + 'px;'
    styleAfter.innerHTML += 'border-top-width: ' + borderLeftWidth * 2 + 'px;';
    styleAfter.innerHTML += 'top: ' + borderLeftWidth * 2 + 'px;';
    styleAfter.innerHTML += 'left: ' + (halfHeight - borderLeftWidth) + 'px;';
    styleAfter.innerHTML += '}';
    document.querySelector('.AudioButtonLoop').appendChild(styleAfter);

    audioButtonPause.style.opacity = "0.9";
    audioButtonPlay.style.opacity = "0.4";
    audioButtonLoop.style.opacity = "0.4";
    //audioButtonLoop.style.opacity = "1"; для отладки

    audioButtons.hidden = "";
    audioButtons.style.display = "table-cell";

    selecteditem.parentElement.appendChild(audioButtons);
  }

}

/* Occurs when the end of playback is reached. */
function LiveAudio_ended() {
  spinner = document.getElementById("spinner");
  spinner.hidden = "hidden";
  spinner.style.display = "none";

  audioButtons = document.getElementById("AudioButtons");
  audioButtons.hidden = "hidden";
  audioButtons.style.display = "none";

  if (!MultimediaItem.loop) {
    LiveAudio_play();
  }  
}

/* Occurs when the download has stopped. */
function LiveAudio_stalled() {
}

/* Fires when an error occurs during object loading.*/
function LiveAudio_onerror() {
  if (MultimediaItem) {
    elementHide("spinner");
    elementHide("AudioButtons");

    if (MultimediaItem.error) {
      switch (MultimediaItem.error.code) {
        case 1: //MEDIA_ERR_ABORTED
          message = "ERROR ABORTED";
          break;
        case 2: //MEDIA_ERR_NETWORK      
          message = "ERROR NETWORK";
          break;
        case 3: //MEDIA_ERR_DECODE
          message = "ERROR DECODE";
          break;
        case 4: //MEDIA_ERR_SRC_NOT_SUPPORTED
          message = "NOT SUPPORTED";
          break;
        default:
          message = "Undefined error code";
          break;
      }

      selecteditem.attributes["savedText"].value = selecteditem.firstChild.textContent;
      selecteditem.style.backgroundColor = ErrorPlaybackgroundColor;
      selecteditem.style.color = "rgb(255, 255, 255)";
      selecteditem.firstChild.textContent = message + ". " + selecteditem.firstChild.textContent;
      selecteditem.attributes["IsError"].value = "1";
      LiveAudio_Play_Next();
    }
  }
}

function AudioButtonPause_click() {
  if (AudioButtonPause.style.opacity != "0.4") {
    MultimediaItem.pause();

    AudioButtonPause.style.opacity = "0.4";
    AudioButtonPlay.style.opacity = "0.9";
  }
}

function AudioButtonPlay_click() {
  if (AudioButtonPlay.style.opacity != "0.4") {
    MultimediaItem.play();

    AudioButtonPause.style.opacity = "0.9";
    AudioButtonPlay.style.opacity = "0.4";
  }
}

function AudioButtonLoop_click() {
  if (AudioButtonLoop.style.opacity != "0.4") {
    AudioButtonLoop.style.opacity = "0.4";
    MultimediaItem.loop = false;
  }
  else {
    MultimediaItem.loop = true;
    AudioButtonLoop.style.opacity = "0.9";
  }
}

/* ==== Multimedia Video Start === */

/* Occurs when the media element is reset to its initial state. */
function LiveVideo_emptied() {
  if (isCloseVideo) return;

  spinnerShow();
  elementHide("videoButtons");
}

/* Occurs when playback stops because the next frame of a video resource is not available. */
function LiveVideo_waiting() {
}

/* Occurs when Internet Explorer begins looking for media data. */
function LiveVideo_loadstart() {
  if (isCloseVideo) return;
}

/* Occurs when media data is loaded at the current playback position. */
function LiveVideo_loadeddata() {
}

/* Occurs when the audio or video has started playing. */
function LiveVideo_playing() {

  selecteditem.attributes["IsPlayed"].value = "1";
  selecteditem.style.backgroundColor = NonPlaybackgroundColor;

  elementHide("spinner");

  var masterTitle = document.getElementById("masterTitle");
  var masterTitleHeight = parseInt(getComputedStyle(masterTitle).height, 10);
  var masterTitlePaddingTop = parseInt(getComputedStyle(masterTitle).paddingTop, 10);
  var masterTitlePaddingBottom = parseInt(getComputedStyle(masterTitle).paddingBottom, 10);

  var breadCrumps = document.getElementById("breadCrumpsDiv");
  var breadCrumpsHeight = parseInt(getComputedStyle(breadCrumps).height, 10);
  var breadCrumpsPaddingTop = parseInt(getComputedStyle(breadCrumps).paddingTop, 10);
  var breadCrumpsPaddingBottom = parseInt(getComputedStyle(breadCrumps).paddingBottom, 10);

  var shiftHeight = 14; //11 - It's in bottom
  videoHeight = window.innerHeight - (masterTitleHeight + masterTitlePaddingTop + masterTitlePaddingBottom + breadCrumpsHeight + breadCrumpsPaddingTop + breadCrumpsPaddingBottom + shiftHeight);

  var panelListMultimedia = document.getElementById("ContentPlaceHolder1_PanelListMultimedia");

  MultimediaItem.style.width = getComputedStyle(panelListMultimedia).width;
  MultimediaItem.style.height = videoHeight + "px";

  //for debug
  //selecteditem.firstChild.textContent = "PanelListMultimedia.clientWidth=" + panelListMultimedia.clientWidth + ", videoHeight=" + videoHeight;

  closeVideo = document.getElementById("closeVideo");
  var computedStylecloseVideo = getComputedStyle(closeVideo);
  closeVideo.style.marginLeft = (parseInt(MultimediaItem.style.width, 10) - parseInt(computedStylecloseVideo.width, 10) - 10) + "px";

  videoButtons = document.getElementById("videoButtons");

  closeVideo.addEventListener("click", function () {
    if (!MultimediaItem.paused) MultimediaItem.pause();
    isCloseVideo = true;

    var LiveMultimedia = document.getElementById("LiveMultimedia");
    LiveMultimedia.appendChild(videoButtons);
    
    //elementHide("videoButtons");    
    MultimediaItem.title = "";
    MultimediaItem.src = "";
  }, false);
 
  videoButtons.hidden = "";
  videoButtons.style.display = "block";

  panelListMultimedia.parentElement.insertBefore(videoButtons, panelListMultimedia);

  if (MultimediaItem.paused) MultimediaItem.play(); // This is for non autoplaying browsers (Chrome, etc.)

  ScrollToTop(this);
}

/* Occurs when the end of playback is reached. */
function LiveVideo_ended() {
}

/* Occurs when the download has stopped. */
function LiveVideo_stalled() {
}

/* Fires when an error occurs during object loading.*/
function LiveVideo_onerror() {
  elementHide("spinner");
  elementHide("videoButtons");

  if (isCloseVideo) {
    isCloseVideo = false;
    return;
  }

  if (MultimediaItem) {
    if (MultimediaItem.error) {
      switch (MultimediaItem.error.code) {
        case 1: //MEDIA_ERR_ABORTED
          message = "ERROR ABORTED";
          break;
        case 2: //MEDIA_ERR_NETWORK      
          message = "ERROR NETWORK";
          break;
        case 3: //MEDIA_ERR_DECODE
          message = "ERROR DECODE";
          break;
        case 4: //MEDIA_ERR_SRC_NOT_SUPPORTED
          message = "NOT SUPPORTED";
          break;
        default:
          message = "Undefined error code";
          break;
      }

      selecteditem.attributes["savedText"].value = selecteditem.firstChild.textContent;
      selecteditem.style.backgroundColor = ErrorPlaybackgroundColor;
      selecteditem.style.color = "rgb(255, 255, 255)";
      selecteditem.firstChild.textContent = message + ". " + selecteditem.firstChild.textContent;
      selecteditem.attributes["IsError"].value = "1";
    }
  }
}
/* ==== Multimedia Video Stop === */

/* Show element */
function elementShow(elementName) {
  var elem = document.getElementById(elementName);
  elem.hidden = "";
  elem.style.display = "table-cell";
}

/* Hide element */
function elementHide(elementName) {
  var elem = document.getElementById(elementName);
  elem.hidden = "hidden";
  elem.style.display = "none";
}

/* Show spinner */
function spinnerShow() {
  spinner = document.getElementById("spinner");
  spinner.hidden = "";
  spinner.style.display = "table-cell";
}

function toggleFullScreen() {
  //alert(document.fullscreenElement); alert(document.mozFullScreenElement); alert(document.webkitFullscreenElement); alert(document.msFullscreenElement);
  if (!document.fullscreenElement && !document.mozFullScreenElement && !document.webkitFullscreenElement && !document.msFullscreenElement) {
    if (document.documentElement.requestFullscreen) {
      document.documentElement.requestFullscreen();
    } else if (document.documentElement.msRequestFullscreen) {
      document.documentElement.msRequestFullscreen();
    } else if (document.documentElement.mozRequestFullScreen) {
      document.documentElement.mozRequestFullScreen();
    } else if (document.documentElement.webkitRequestFullscreen) {
      document.documentElement.webkitRequestFullscreen(Element.ALLOW_KEYBOARD_INPUT);
    }
  } else {
    if (document.exitFullscreen) {
      document.exitFullscreen();
    } else if (document.msExitFullscreen) {
      document.msExitFullscreen();
    } else if (document.mozCancelFullScreen) {
      document.mozCancelFullScreen();
    } else if (document.webkitExitFullscreen) {
      document.webkitExitFullscreen();
    }
  }


  //MultimediaItem.addEventListener('click', function () {
  //  alert(1);
  //  multimediaItem = document.getElementById("livevideo");
  //  alert(2);
  //  if (multimediaItem.paused) {
  //    alert(3);
  //    multimediaItem.play();
  //  }
  //  else {
  //    alert(4);
  //    multimediaItem.pause();
  //  }
  //}, false);

  //if (!document.documentElement.msRequestFullscreen) {
  //  MultimediaItem.addEventListener('dblclick', function () {
  //    toggleFullScreen();
  //    //if (MultimediaItem.requestFullscreen) {
  //    //  MultimediaItem.requestFullscreen();
  //    //} else if (MultimediaItem.msRequestFullscreen) {
  //    //  MultimediaItem.msRequestFullscreen();
  //    //} else if (MultimediaItem.mozRequestFullScreen) {
  //    //  MultimediaItem.mozRequestFullScreen();
  //    //} else if (MultimediaItem.webkitRequestFullscreen) {
  //    //  MultimediaItem.webkitRequestFullscreen();
  //    //}
  //  }, false);
  //}

}

function SetStateControl(stateControl) {
}

function SelectMultimediaItem(event) {
  event = event || window.event;
  PressedItem = event;  

  LiveAudio_Play_Next();
}

function ReceiveServerData(rValue) {
  // Nothing To Do
}

function ScrollToTop(event) {
  window.scrollTo(0, 0);
}
