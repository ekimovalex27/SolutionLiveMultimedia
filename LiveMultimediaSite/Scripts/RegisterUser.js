function LockScreenOnProcessing(lockTitle1, lockTitle2) {
  var FirstName = document.getElementById("ContentPlaceHolder1_tbFirstName");
  var LastName = document.getElementById("ContentPlaceHolder1_tbLastName");
  var Username = document.getElementById("ContentPlaceHolder1_tbUsername");
  var Password = document.getElementById("ContentPlaceHolder1_tbPassword");
  var PasswordReType = document.getElementById("ContentPlaceHolder1_tbPasswordReType");

  if (FirstName && LastName && Username && Password && PasswordReType) {
    if (isGoodString(FirstName.value) && isGoodString(LastName.value) && isGoodString(Username.value) && isGoodString(Password.value) && isGoodString(PasswordReType.value)) {
      if (/.+@.+\..+/i.test(Username.value)) { //Check format e-mail
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
}

function isGoodString(source) {
  if (source==null) return false;

  for (var i = 0; i < source.length; i++)
    if (source.charAt(i) !=  " ")
      return true;

  return false;
}
