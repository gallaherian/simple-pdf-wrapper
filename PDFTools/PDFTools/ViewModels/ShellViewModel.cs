using System;
using Caliburn.Micro;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using System.IO;

namespace PDFTools.ViewModels
{
    public class ShellViewModel : Caliburn.Micro.Screen
    {
        #region Properties

        private string m_deleteOldPDF = "";
        public string DeleteOldPDF
        {
            get
            {
                return m_deleteOldPDF;
            }
            set
            {
                m_deleteOldPDF = value;
                NotifyOfPropertyChange(() => DeleteOldPDF);
            }
        }

        private string m_savePDF = "";
        public string SavePDF
        {
            get
            {
                return m_savePDF;
            }
            set
            {
                m_savePDF = value;
                NotifyOfPropertyChange(() => SavePDF);
            }
        }

        private string m_pages = "";
        public string Pages
        {
            get
            {
                return m_pages;
            }
            set
            {
                m_pages = Regex.Replace(value, "[^0-9]", ",");
                NotifyOfPropertyChange(() => Pages);
            }
        }

        private string m_status = "DarkGreen";
        public string Status
        {
            get
            {
                return m_status;
            }
            set
            {
                m_status = value;
                NotifyOfPropertyChange(() => Status);
            }
        }

        public string FolderIcon
        {
            get
            {
                string path = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location) + @"\folder.png";
                return path;
            }
        }

        public bool m_isDeleteSelected = true;
        public bool IsDeleteSelected
        {
            get
            {
                return m_isDeleteSelected;
            }
            set
            {
                if (m_isDeleteSelected != value)
                    Status = "DarkGreen";

                m_isDeleteSelected = value;
                NotifyOfPropertyChange(() => IsDeleteSelected);
                NotifyOfPropertyChange(() => RunString);
            }
        }

        public bool m_isCombineSelected = false;
        public bool IsCombineSelected
        {
            get
            {
                return m_isCombineSelected;
            }
            set
            {
                m_isCombineSelected = value;
                NotifyOfPropertyChange(() => IsCombineSelected);
                NotifyOfPropertyChange(() => RunString);
            }
        }

        public string RunString
        {
            get
            {
                if (IsCombineSelected)
                {
                    return "Combine PDFs";
                }
                else
                {
                    return "Delete Pages";
                }
            }
        }

        public string CombineSelectedItem { get; set; }

        private BindableCollection<string> m_combineList = new BindableCollection<string>();
        public BindableCollection<string> CombineList
        {
            get
            {
                return m_combineList;
            }
            set
            {
                m_combineList = value;
                NotifyOfPropertyChange(() => CombineList);
            }
        }

        #endregion Properties

        #region Functions
        public void CombineAdd()
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "pdf file (*.pdf)|*.pdf|All files (*.*)|*.*";
                openFileDialog.FilterIndex = 1;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    CombineList.Add(openFileDialog.FileName);
                }
            }
        }
        public void CombineRemove()
        {
            if (!string.IsNullOrEmpty(CombineSelectedItem))
                CombineList.Remove(CombineSelectedItem);
        }

        public void CombineUp()
        {
            if (!string.IsNullOrEmpty(CombineSelectedItem))
            {
                string tmp = CombineSelectedItem;
                int idx = CombineList.IndexOf(CombineSelectedItem);
                if (idx != 0)
                {
                    CombineList.RemoveAt(idx);
                    CombineList.Insert(idx - 1, tmp);

                    NotifyOfPropertyChange(() => CombineList);
                    CombineSelectedItem = tmp;
                    NotifyOfPropertyChange(() => CombineSelectedItem);
                }
            }
            
        }
        public void CombineDown()
        {
            if (!string.IsNullOrEmpty(CombineSelectedItem))
            {
                string tmp = CombineSelectedItem;
                int idx = CombineList.IndexOf(CombineSelectedItem);
                if (idx != (CombineList.Count - 1))
                {
                    CombineList.RemoveAt(idx);
                    CombineList.Insert(idx + 1, tmp);

                    NotifyOfPropertyChange(() => CombineList);
                    CombineSelectedItem = tmp;
                    NotifyOfPropertyChange(() => CombineSelectedItem);
                }
            }
            NotifyOfPropertyChange(() => CombineList);
        }

        public void Run()
        {
            try
            {
                if (IsCombineSelected)
                {
                    Combine();
                }
                else
                {
                    Delete();
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        public void Combine()
        {
            if (string.IsNullOrEmpty(SavePDF) || CombineList.Count == 0 || IsFileLocked(SavePDF))
            {
                Status = "DarkRed";
                return;
            }

            string arguments = "";
            foreach (var file in CombineList)
            {
                if (!File.Exists(file))
                {
                    Status = "DarkRed";
                    return;
                }
                arguments += " \"" + file + "\"";
            }

            Process.Start("pdftk", arguments + " output \"" + SavePDF + "\"");
            Status = "DarkGreen";
            SavePDF = "";
        }

        public void Delete()
        {
            if (String.IsNullOrEmpty(Pages) || String.IsNullOrEmpty(SavePDF) || String.IsNullOrEmpty(DeleteOldPDF) || IsFileLocked(SavePDF) || !File.Exists(DeleteOldPDF))
            {
                Status = "DarkRed";
                return;
            }

            var pageArray = m_pages.Split(',');
            var pageNos = new List<int>(Array.ConvertAll(pageArray, x => int.Parse(x)));

            // Get the number of pages
            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "pdftk",
                    Arguments = "\"" + DeleteOldPDF + "\" data_dump",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            int totalPages = 0;
            proc.Start();
            while (!proc.StandardOutput.EndOfStream)
            {
                string line = proc.StandardOutput.ReadLine();
                if (line.Contains("NumberOfPages"))
                {
                    var tmp = line.Split(':');
                    totalPages = int.Parse(tmp[1]);
                }
            }

            // Remove pages after the end
            pageNos.RemoveAll(x => x > totalPages);
            if (pageNos.Count == 0)
                return;

            pageNos.Sort();

            // Get pages left
            var keepPages = new List<int>();
            for (int i = 1; i <= totalPages; i++)
            {
                if (!pageNos.Contains(i))
                    keepPages.Add(i);
            }

            // Remove the pages in the middle
            var keepBoundaries = new List<int>();
            keepBoundaries.Add(keepPages[0]);
            keepBoundaries.Add(keepPages[0]);
            for (int i = 1; i < keepPages.Count; i++)
            {
                if ((keepPages[i] - keepPages[i - 1]) == 1)
                {
                    keepBoundaries[keepBoundaries.Count - 1] = keepPages[i];
                }
                else
                {
                    keepBoundaries.Add(keepPages[i]);
                    keepBoundaries.Add(keepPages[i]);
                }
            }

            string pageString = "";
            for (int i = 0; i < keepBoundaries.Count; i += 2)
            {
                pageString += keepBoundaries[i] + "-" + keepBoundaries[i + 1] + " ";
            }
            pageString = pageString.TrimEnd();

            Process.Start("pdftk", "\"" + DeleteOldPDF + "\" cat " + pageString + " output \"" + SavePDF);
            Status = "DarkGreen";
            SavePDF = "";
        }

        protected virtual bool IsFileLocked(string fileName)
        {
            if (!File.Exists(fileName))
                return false;

            try
            {
                var file = new FileInfo(fileName);
                using (FileStream stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    stream.Close();
                }
            }
            catch (IOException e)
            {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                return true;
            }

            //file is not locked
            return false;
        }

        public void SelectDeleteOldPDF()
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "pdf file (*.pdf)|*.pdf|All files (*.*)|*.*";
                openFileDialog.FilterIndex = 1;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    //Get the path of specified file
                    DeleteOldPDF = openFileDialog.FileName;
                }
            }
        }

        public void SelectSavePDF()
        {
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "pdf file (*.pdf)|*.pdf|All files (*.*)|*.*";
                saveFileDialog.FilterIndex = 1;
                saveFileDialog.RestoreDirectory = true;

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    //Get the path of specified file
                    SavePDF = saveFileDialog.FileName;
                }
            }
        }

        #endregion Functions
    }
}
