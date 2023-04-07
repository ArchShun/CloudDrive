using CloudDrive.Utils;
using CloudDriveUI.Models;
using CloudDriveUI.Utils;

namespace Test;

internal class TestCloudDriveUI 
{
    //[TestMethod]
    public static void TestGetLocalFileInfos()
    {
        string root = @"C:\Test\";
        string path = @"C:\Test\123\abc\text.txt";
        var str = System.IO.Path.GetRelativePath(root, path);
        Console.WriteLine(str);
        Console.WriteLine("-------------------------");

        root = @"Test\";
        path = @"Test\123\abc\text.txt";
        str = System.IO.Path.GetRelativePath(root, path);
        Console.WriteLine(str);
        Console.WriteLine("-------------------------");

        path = @"Test\123\abc/text/";
        str = System.IO.Path.TrimEndingDirectorySeparator( path);
        Console.WriteLine(str);
        Console.WriteLine("-------------------------");

    }


}
