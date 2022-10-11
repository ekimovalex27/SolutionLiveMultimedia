using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;

namespace LiveMultimediaREST.Test
{
  [TestClass]
  public class WhenWeCallLiveMultimediaREST
  {
    [TestMethod][ExpectedException(typeof(System.ArgumentNullException))]
    public void ShouldReturnExceptionWhenUriIsNull()
    {
      var rest = new REST(null, null, null);

      var size = 1;
      string result = "test";

      var a = rest.GetMultimediaItemSourceOneDrive(null, out size);
    }


   // 1. get input data

    //  2. validate it
     //   3. save into db

    [TestMethod]

    public void CheckOneDrive()
    {

      var rest = MockRepository.GenerateMock<IREST>();
      //var restdb = MockRepository.GenerateMock<IRESTDB>();
      var size = 1;
      string result = "test";
      rest.Expect(x => x.GetMultimediaItemSourceOneDrive(null, out size)).Return(result);
      
      var a = rest.GetMultimediaItemSourceOneDrive(null, out size);

      Assert.AreEqual(a, "test");
      Assert.IsTrue(a.Length == 4);
    }
  }
}
