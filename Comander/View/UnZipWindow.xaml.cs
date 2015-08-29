using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Comander.CommanderIO;
using ZipLib;

namespace Comander.View
{
    public enum ExtractType
    {
        Here,
        ToSpecificDir
    }

    public partial class UnZipWindow : Window
    {
        private readonly IMetadataFileStructure _file;
        private readonly Dictionary<string, ZipType> _typeDict;
        private ExtractType _extractionType = ExtractType.Here;

        public UnZipWindow(IMetadataFileStructure file)
        {
            _file = file;
            _typeDict = new Dictionary<string, ZipType>
            {
                {"zip", ZipType.Zip},
                {"7z", ZipType.SevenZip},
                {"tar", ZipType.Tar},
                {"gzip", ZipType.GZip},
                {"bzip2", ZipType.Bzip2}
            };
            InitializeComponent();
            ExtractDirRadio.Content = PerformName();
        }

        private string PerformName()
        {
            string ext = _file.Ext;
            return "Extract to "+_file.Name.Substring(0, _file.Name.Length - ext.Length );
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            ZipType zipType = GetArchiveType();
            if (!string.IsNullOrEmpty(PasswordBox.Password) && !string.IsNullOrEmpty(PasswordBox1.Password))
            {
                if (PasswordBox.Password.Equals(PasswordBox1.Password))
                {
                    ZipParameters = new ZipParameters( _file.FullName, PasswordBox.Password, zipType);
                    Close();
                }
                else
                {
                    StatusTextBlock.Text = "Passwords are diffrent";
                }
            }
            else
            {
                ZipParameters = new ZipParameters( _file.FullName, string.Empty, zipType);
                Close();
            }
        }

        public ZipParameters ZipParameters { get; set; }

        public ExtractType ExtractionType
        {
            get { return _extractionType; }
        }

        private ZipType GetArchiveType()
        {
            try
            {
                string ext = _file.Ext.Replace(".","");
                return _typeDict[ext];
            }
            catch
            {
                throw new ArgumentException("File is not archive");
            }
        }

        private void ToggleButton_OnChecked(object sender, RoutedEventArgs e)
        {
            RadioButton checkBox = sender as RadioButton;
            if (checkBox != null)
            {
                _extractionType = checkBox.Name.Equals("ExtractDirRadio") ? ExtractType.ToSpecificDir : ExtractType.Here;
            }
        }
    }
}
