using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Browsing_History_Capturer
{
    class Program
    {
        static void Main(string[] args)
        {
            start_capturing();
        }
        /// <summary>
        /// Get ------HistoryFileLocation from  get_    _history_file_location method and pass -------CommandText as reference
        /// check if file exists
        /// call get_history method and pass "history file location", queryString(commandText) and "Browser name"
        /// </summary>
        static void start_capturing()
        {
            //Flush Temporary History Folder
            string tempDirectoryPath = Application.StartupPath + @"\TemporaryHistory\";
            if (!FlushTemporaryDirectory(tempDirectoryPath))
            {
                return;
            }
            //commandText(Query) variable 
            string chromeCommandText = "",firefoxCommandText = "", operaCommandText = "";

            //getting chrome history
            string chromeHistoryFileLocation = get_chrome_history_file_location(out chromeCommandText);
            if (!(chromeHistoryFileLocation.Equals("")))
            {
                get_history(chromeHistoryFileLocation, chromeCommandText, "Google Chrome", "Google Chrome");
            }
            //getting firefox history
            string firefoxHistoryFileLocation = get_firefox_history_file_location(out firefoxCommandText);
            if (!(firefoxHistoryFileLocation.Equals("")))
            {
                get_history(firefoxHistoryFileLocation, firefoxCommandText, "Mozila Firefox", "Mozila Firefox");
            }
            //get_opera_history_file_location
            string operaHistoryFileLocation = get_opera_history_file_location(out operaCommandText);
            if (!(operaHistoryFileLocation.Equals("")))
            {
                get_history(operaHistoryFileLocation, operaCommandText, "Opera", "Opera");
            }
        }
        #region HistoryFilesGetter Methods
        /// <summary>
        /// Google Chrome stores browser history in a file named "History" inside "\Google\Chrome\User Data\" directory in LocalApplicationData
        /// This method contain path to directory containing chrome browser history file inside LocalApplicationData and history file name
        /// Set query text to "query" passed as reference
        /// call FindFile function and pass directory path and file name
        /// return FindFile's returned value
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        private static string get_chrome_history_file_location(out string query)
        {
            query = "select urls.url, urls.title,datetime((visits.visit_time/1000000)-11644473600, 'unixepoch') as Visit_time from urls Join visits on urls.id=visits.url order by Visit_time desc";
            string google = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Google\Chrome\User Data\";
            string googleHistoryFileName = "History";
            return FindFile(google, googleHistoryFileName);
        }
        /// <summary>
        /// Firefox stores browser history in a file named "places.sqlite" inside "\Mozilla\Firefox\Profiles\" directory in ApplicationData
        /// This method contain path to directory containing browser history file inside ApplicationData and history file name
        /// Set query text to "query" passed as reference
        /// call FindFile function and pass directory path and file name
        /// return FindFile's returned value
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        private static string get_firefox_history_file_location(out string query)
        {
            query = "select moz_places.url, moz_places.title,datetime(moz_historyvisits.visit_date/1000000,'unixepoch') as Visit_time from moz_places join moz_historyvisits on moz_places.id=moz_historyvisits.place_id order by Visit_time desc";
            string directory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Mozilla\Firefox\Profiles\";
            string fileName = "places.sqlite";
            return FindFile(directory, fileName);
        }
        /// <summary>
        /// Opera stores browser history in a file named "History" inside "\Opera Software\" directory in ApplicationData
        /// This method contain path to directory containing browser history file inside ApplicationData and history file name
        /// Set query text to "query" passed as reference
        /// call FindFile function and pass directory path and file name
        /// return FindFile's returned value
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        private static string get_opera_history_file_location(out string query)
        {
            query = "select urls.url, urls.title,datetime((visits.visit_time/1000000)-11644473600,'unixepoch') as Visit_time from urls join visits on urls.id=visits.url";
            string directory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\Opera Software\";
            string fileName = "History";
            return FindFile(directory, fileName);
        }
        #endregion
        /// <summary>
        /// The method will firstly create a duplicate of browser history file named "tempHistoryFile" inside project working directory to avoid "File already in use" exception.
        /// Then it'll create a sqllite connection and executes query.
        /// Print extracted data from that file
        /// Delete tempHistoryFille
        /// </summary>
        /// <param name="path"></param>
        /// <param name="query"></param>
        /// <param name="browser"></param>
        private static void get_history(string path, string query, string browser, string tempFileName)
        {
            try
            {
                //Creating a temporary directory
                string tempDirectoryPath = Application.StartupPath + @"\TemporaryHistory\";
                //temp file to store history related data copied from browser's database
                string fileName = tempDirectoryPath + tempFileName;
                
                File.Copy(path, fileName,true);
                Console.WriteLine("\n**********************************************     "+browser+"      **********************************************\n");
                using (SQLiteConnection con = new SQLiteConnection("DataSource = " + fileName + ";Versio=3;New=False;Compress=True;"))
                {
                    con.Open();
                    SQLiteCommand cmd = new SQLiteCommand();
                    cmd.Connection = con;
                    cmd.CommandText = query;
                    SQLiteDataReader dr = cmd.ExecuteReader();
                    while (dr.Read())
                    {
                        if (!(dr["title"].ToString().Equals("")))
                        {
                            Console.WriteLine("\n\nURL :{0}\nTitle :{1}\nVisit_Time{2}\nBrowser :{3}", dr["url"].ToString(), dr["title"].ToString(), Convert.ToDateTime(dr["Visit_time"].ToString()), browser);
                        }
                    }
                    con.Close();
                    Console.WriteLine("\n********************************************************************************************\n");
                }
            }
            catch
            {
                return;
            }
        }
        /// <summary>
        /// This method will search file inside the given directory(including subdirectories) and return file path if found else empty string
        /// </summary>
        /// <param name="directory">Directory Path from where file to be searched</param>
        /// <param name="fileName">File name to be searched</param>
        /// <returns></returns>
        private static string FindFile(string directory, string fileName)
        {
            var file = Directory.GetFiles(directory, fileName, SearchOption.AllDirectories)
                    .FirstOrDefault();
            if (file == null)
            {
                return "";
            }
            else
            {
                return Path.GetFullPath(file);
            }
        }
        /// <summary>
        /// Delete temp directory if already exists and create/rectreate it
        /// this method will delete previosuly created temp files
        /// </summary>
        private static bool FlushTemporaryDirectory(string tempDirectory)
        {
            try
            {
                string startupPath = tempDirectory;
                if (Directory.Exists(startupPath))
                {
                    System.IO.DirectoryInfo dInfo = new DirectoryInfo(startupPath);
                    foreach (FileInfo file in dInfo.GetFiles())
                    {
                        file.Delete();
                    }
                }
                DirectoryInfo di = Directory.CreateDirectory(Path.GetDirectoryName(tempDirectory));
                di.Attributes = FileAttributes.Directory | FileAttributes.Hidden;
                return true;
            }
            catch
            {
                MessageBox.Show("Exception in FlushTemporaryDirectory: ");
                return false;
            }
        }
    }
}
