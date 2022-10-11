using System;

public static class LiveMultimediaLibrary
{
  public static string ConvertObjectToString(Object ConvertObject)
  {
    string stringObject;

    if (ConvertObject == null)
      stringObject = "";
    else
      stringObject = ConvertObject.ToString();

    if (string.IsNullOrEmpty(stringObject) || string.IsNullOrWhiteSpace(stringObject)) stringObject = "";

    return stringObject;
  }

  public static bool CheckGoodString(string CheckString)
  {
    bool IsSuccess = (!string.IsNullOrEmpty(CheckString) && !string.IsNullOrWhiteSpace(CheckString));
    return IsSuccess;
  }

  public static bool CheckEmptyString(string CheckString)
  {
    bool IsSuccess = (string.IsNullOrEmpty(CheckString) || string.IsNullOrWhiteSpace(CheckString));
    return IsSuccess;
  }

  //public int ConvertObjectToInt(Object ConvertObject)
  //{
  //  string intObject;

  //  if (ConvertObject == null)
  //    intObject = 0;
  //  else
  //    intObject = ConvertObject.ToString();

  //  if (string.IsNullOrEmpty(intObject) || string.IsNullOrWhiteSpace(intObject)) intObject = "";

  //  return intObject;
  //}

}

//[DataContract]
//public class MultimediaStream : MemoryStream, IDisposable
//{
//  private long length = 0;
//  private long position = 0;

//  private long range1, range2;

//  public MultimediaStream(long Range1, long Range2)
//  {
//    range1 = Range1;
//    range2 = Range2;
//  }

//  [DataMember]
//  public override bool CanRead
//  {
//    get
//    {
//      return true;
//    }
//  }

//  [DataMember]
//  public override bool CanWrite
//  {
//    get
//    {
//      return true;
//    }
//  }

//  [DataMember]
//  public override bool CanSeek
//  {
//    get
//    {
//      return false;
//    }
//  }

//  [DataMember]
//  public override long Length
//  {
//    get
//    {
//      return length;
//    }
//  }

//  [DataMember]
//  public override long Position
//  {
//    get
//    {
//      return position;
//    }

//    set
//    {
//      position = value;
//    }
//  }

//  public override int Read(byte[] buffer, int offset, int count)
//  {
//    try
//    {
//      //var FullPath = @"\\192.168.1.254\volume_1\Multimedia\video\Фильмы\Казино Рояль.mp4";
//      var FullPath = @"C:\LiveMultimediaDemo\Фильмы\Казино Рояль.mp4";
//      //var FullPath = @"C:\LiveMultimediaDemo\Фильмы\Вспомнить всё.mp4";
//      //var FullPath = @"C:\LiveMultimediaDemo\Фильмы\huge.mp4";
//      //var FullPath = @"C:\LiveMultimediaDemo\Мульфильмы\Бременские музыканты\Бременские музыканты.mp4";
//      //var FullPath = @"C:\LiveMultimediaDemo\Мульфильмы\Винни-Пух\3 Винни-Пух и день забот.mp4";
//      System.IO
//      using (var fs = new FileStream(FullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 8, FileOptions.Asynchronous))
//      {
//        var MultimediaFileLength = fs.Length;

//        var MultimediaFileBufferLength = count;
//        fs.Seek(offset, SeekOrigin.Begin);
//        buffer = new byte[MultimediaFileBufferLength];
//        fs.Read(buffer, 0, MultimediaFileBufferLength);
//      }
//    }
//    catch (Exception)
//    {
//      buffer = null;
//    }


//    return 0;
//  }

//  public override void Write(byte[] buffer, int offset, int count)
//  {
//    length = buffer.Length;
//    base.Write(buffer, offset, count);    
//  }

//  public override long Seek(long offset, SeekOrigin origin)
//  {
//    return 0;
//  }

//  public override void SetLength(long value)
//  {
//  }

//  public override void Flush()
//  {

//  }

//  protected override void Dispose(bool disposing)
//  {
//    base.Dispose(disposing);
//  }
//}
