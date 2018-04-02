using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using RFCOMM_OBEX;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace RFCOMM_OBEX
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private OBEX_Sender sndr;
        private OBEX_Receiver rcvr;
        public MainPage()
        {
            this.InitializeComponent();


        }

        private async Task PickAFile()
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary;
            picker.FileTypeFilter.Add(".txt");

            Windows.Storage.StorageFile file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                // Application now has read/write access to the picked file
                this.textBlock.Text = "Picked textfile: " + file.Name;
                txt = await Windows.Storage.FileIO.ReadTextAsync(file);
                filename = file.Name;
                await sndr.Send(txt, filename);
            }
            else
            {
                this.textBlock.Text = "Operation cancelled.";
            }
        }

        private async Task SaveAFile(string txt)
        {
            var savePicker = new Windows.Storage.Pickers.FileSavePicker();
            savePicker.SuggestedStartLocation =
                Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
            // Dropdown of file types the user can save the file as
            savePicker.FileTypeChoices.Add("Plain Text", new List<string>() { ".txt" });
            // Default file name if the user does not type one in or select a file to replace
            savePicker.SuggestedFileName = "New Document";

            Windows.Storage.StorageFile file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                // Prevent updates to the remote version of the file until
                // we finish making changes and call CompleteUpdatesAsync.
                Windows.Storage.CachedFileManager.DeferUpdates(file);
                // write to file
                FileDetail rcvFile = await rcvr.ReadAsync();
                await Windows.Storage.FileIO.WriteTextAsync(file, rcvFile.txt);
                // Let Windows know that we're finished changing the file so
                // the other app can update the remote version of the file.
                // Completing updates may require Windows to ask for user input.
                Windows.Storage.Provider.FileUpdateStatus status =
                    await Windows.Storage.CachedFileManager.CompleteUpdatesAsync(file);
                if (status == Windows.Storage.Provider.FileUpdateStatus.Complete)
                {
                    this.textBlock.Text = "File " + file.Name + " was saved.";
                    
                }
                else
                {
                    this.textBlock.Text = "File " + file.Name + " couldn't be saved.";
                }
            }
            else
            {
                this.textBlock.Text = "Operation cancelled.";
            }
        }

        string txt { get; set; } = "";
        string filename { get; set; } = "";

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            await PickAFile();
        }

        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            await SaveAFile(txt);
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            sndr = new OBEX_Sender();
            rcvr = new OBEX_Receiver();
            await sndr.Initialize();
            await rcvr.Initialize();
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {

        }

        private async void Button_Click_3(object sender, RoutedEventArgs e)
        {
            
        }
    }
}
