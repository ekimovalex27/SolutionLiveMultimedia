function LockScreenOnProcessing(lockTitle1, lockTitle2) {
  var OldPassword = document.getElementById("ContentPlaceHolder1_tbOldPassword");
  var NewPassword1 = document.getElementById("ContentPlaceHolder1_tbNewPassword1");
  var NewPassword2 = document.getElementById("ContentPlaceHolder1_tbNewPassword2");

  if (OldPassword && NewPassword1 && NewPassword2) {
    if (isGoodString(OldPassword.value) && isGoodString(NewPassword1.value) && isGoodString(NewPassword2.value)) {
      var lockScreen = document.getElementById("lockScreenPanel");
      if (lockScreen) {
        lockScreen.className = "LockScreenOn";

        var lockScreenTitle1 = document.getElementById("lockScreenTitle1");
        lockScreenTitle1.innerHTML = lockTitle1;

        var lockScreenTitle2 = document.getElementById("lockScreenTitle2");
        lockScreenTitle2.innerHTML = lockTitle2;

        var spinner = document.getElementById("spinner");
        spinner.style.marginTop = "0px";
        spinner.style.height = "150px";
        spinner.style.opacity = "1";
      }
    }
  }
}

function isGoodString(source) {
  if (source==null) return false;

  for (var i = 0; i < source.length; i++)
    if (source.charAt(i) !=  " ")
      return true;

  return false;
}
