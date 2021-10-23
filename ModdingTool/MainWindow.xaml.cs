using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ModdingTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public List<Asset> existingAssets;

        public string modfolder;

        public MainWindow()
        {
            InitializeComponent();

            directory.Text = @"C:\Program Files (x86)\Steam\steamapps\common\Unturned\Maps\420assets\Bundles\420wr\Misturned\Large";

            assetprefix.Text = "MST";
            assetname.Text = "Test";
            number.Text = "1";

            english.Text = "";
            filename.Text = "";

            idmin.Text = "28000";
            idmax.Text = "28999";

            lodmesh.IsChecked = true;

            modfolder = @"C:\Program Files (x86)\Steam\steamapps\common\Unturned\Maps\420assets\Bundles\420wr\Misturned";

            existingAssets = new List<Asset>();

            ReloadAssetList();

            foreach (var asset in existingAssets)
            {
                Trace.WriteLine("Found short ID: " + asset.shortID);
            }

            RegenerateNames();
        }

        private void create_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ReloadAssetList();

                bool shouldNotChangeID = false;

                int newID = GenerateUniqueShortID();

                if (newID == 0)
                {
                    console.Foreground = new SolidColorBrush("#ff6b6b".ToColor());
                    console.Text = "Could not parse ID range";
                    return;
                }
                if (newID == 1)
                {
                    console.Foreground = new SolidColorBrush("#ff6b6b".ToColor());
                    console.Text = "All IDs in that range are taken";
                    return;
                }

                Asset current = existingAssets.Find(a => a.directory == directory.Text + "\\" + filename.Text + "\\" + filename.Text + ".dat");  

                if (current == null)
                {
                    Asset sameID = existingAssets.Find(a => a.shortID == newID);

                    if (sameID != null)
                    {
                        console.Foreground = new SolidColorBrush("#ff6b6b".ToColor());
                        console.Text = "ID " + newID + " is already taken by " + sameID.directory.Substring(sameID.directory.LastIndexOf("\\") + 1);
                        return;
                    }
                }
                else
                {
                    if (current.shortID != newID)
                    {
                        newID = current.shortID;
                        shouldNotChangeID = true;
                    }
                }

                DirectoryInfo directoryInfo = Directory.CreateDirectory(directory.Text + "\\" + filename.Text);

                FileStream assetFile = File.Create(directory.Text + "\\" + filename.Text + "\\" + filename.Text + ".dat");

                string guid = Guid.NewGuid().ToString().Replace("-", "");
                string assetType = ((ListBoxItem)type.SelectedItem).Content.ToString();

                string data =
                    $"GUID {guid}\n" +
                    $"Type {assetType}\n" +
                    $"ID {newID}\n" +
                    "\n" +
                    $"{(assetType == "Decal" ? "Decal_X 3\nDecal_Y 3\nDecal_LOD_Bias 1\n" : "\n")}" +
                    $"{((bool)lodmesh.IsChecked ? "LOD Mesh" : "")}\n" +
                    $"{(palette.Text != "" ? "Material_Palette " + palette.Text : "")}\n"
                ;

                byte[] write = new UTF8Encoding(true).GetBytes(data);
                assetFile.Write(write, 0, write.Length);

                assetFile.Close();

                existingAssets.Add(new Asset(directory.Text + "\\" + filename.Text + "\\" + filename.Text + ".dat", guid, newID));

                FileStream englishFile = File.Create(directory.Text + "\\" + filename.Text + "\\English.dat");
                write = new UTF8Encoding(true).GetBytes("Name " + english.Text);
                englishFile.Write(write, 0, write.Length);
                englishFile.Close();


                console.Foreground = new SolidColorBrush("#6bff77".ToColor());

                if (!shouldNotChangeID)
                {
                    console.Text = "successfully created asset: " + filename.Text + " - " + newID;
                }
                else
                {
                    console.Text = "successfully overwritten asset: " + filename.Text + " - " + newID;
                }
            }
            catch (Exception ex)
            {
                console.Foreground = new SolidColorBrush("#ff6b6b".ToColor());
                console.Text = ex.Message;
            }
        }

        private void number_increase_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(number.Text, out int num))
            {
                number.Text = (num + 1).ToString();
            }

            RegenerateNames();
        }

        private void number_decrease_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(number.Text, out int num))
            {
                if (num >= 1)
                    number.Text = (num - 1).ToString();
            }

            RegenerateNames();
        }

        public void ReloadAssetList()
        {
            existingAssets.Clear();

            if (!int.TryParse(idmin.Text, out int minid))
                return;

            if (!int.TryParse(idmax.Text, out int maxid))
                return;

            var datFiles = Directory.EnumerateFiles($"{modfolder}", "*.dat", SearchOption.AllDirectories);

            StreamReader idChecker;

            foreach (var datFile in datFiles)
            {
                if (!datFile.Contains("English"))
                {
                    //Trace.WriteLine("Found asset file: " + datFile);

                    idChecker = File.OpenText(datFile);

                    try
                    {
                        string assetData = idChecker.ReadToEnd();

                        Match GUIDMatch = Regex.Match(assetData, @"GUID (.+?)\n");
                        Match IDMatch = Regex.Match(assetData, @"\nID (.+?)\n");
                        if (IDMatch.Success)
                        {
                            string GUID = GUIDMatch.Value.Split(' ')[1];
                            string shortID = IDMatch.Value.Split(' ')[1];

                            if (int.TryParse(shortID, out int result))
                            {
                                if (result >= minid && result <= maxid)
                                {
                                    existingAssets.Add(new Asset(datFile, GUID, result));
                                }
                            }
                        }

                        idChecker.Close();
                    }
                    catch (Exception ex)
                    {
                        console.Foreground = new SolidColorBrush("#ff6b6b".ToColor());
                        console.Text = "Asset Read Error: " + ex.Message;
                        idChecker.Close();
                    }
                }
            }

            existingAssets.Sort((a1, a2) => a1.shortID.CompareTo(a2.shortID));
        }

        public int GenerateUniqueShortID()
        {
            if (!int.TryParse(idmin.Text, out int minid))
                return 0;

            if (!int.TryParse(idmax.Text, out int maxid))
                return 0;

            if (existingAssets.Count == 0)
                return minid;

            int index = 0;

            for (int newid = minid; newid <= maxid; newid++)
            {
                if (index >= existingAssets.Count || newid < existingAssets[index].shortID)
                {
                    return newid;
                }
                else
                    index++;
            }
            return 1;
        }
        public string GenerateAssetName(string name, string prefix = "", string number = "0")
        {
            string output = "";
            if (prefix != "")
                output += prefix.ToUpper() + "_";
            output += name.Replace(" ", "_");
            if (int.TryParse(number, out int result) && result != 0)
                output += "_" + number;

            return output;
        }
        public string GenerateAssetEnglish(string name, string prefix = "", string number = "0")
        {
            string output = "";
            if (prefix != "")
                output += prefix.ToUpper() + " ";
            output += name.Replace("_", " ");
            if (int.TryParse(number, out int result) && result != 0)
                output += " #" + number;

            return output;
        }
        public void RegenerateNames()
        {
            filename.Text = GenerateAssetName(assetname.Text, assetprefix.Text, number.Text);
            english.Text = GenerateAssetEnglish(assetname.Text, assetprefix.Text, number.Text);
        }

        private void assetname_TextChanged(object sender, TextChangedEventArgs e)
        {
            RegenerateNames();
        }

        private void assetprefix_TextChanged(object sender, TextChangedEventArgs e)
        {
            RegenerateNames();
        }
    }

    public class Asset
    {
        public string directory;
        public string GUID;
        public int shortID;

        public Asset(string directory, string gUID, int shortID)
        {
            this.directory = directory;
            GUID = gUID;
            this.shortID = shortID;
        }
    }
}
