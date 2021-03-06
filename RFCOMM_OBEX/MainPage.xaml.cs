﻿using System;
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
using Windows.Storage.Pickers;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace RFCOMM_OBEX
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private OBEX_Sender sndr= null;
        private OBEX_Receiver rcvr= null;
        public MainPage()
        {
            this.InitializeComponent();
            root = this;
        }

        private async Task SendAFile()
        {
            if (sndr == null)
            {
                sndr = new OBEX_Sender();
            }

            FileOpenPicker picker = null;
#if IoTCore
#else
            picker = new Windows.Storage.Pickers.FileOpenPicker();
#endif
            if (picker != null)
            {
                picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
                picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary;
                picker.FileTypeFilter.Add(".txt");

                Windows.Storage.StorageFile file = await picker.PickSingleFileAsync();
                if (file != null)
                {
                    PostMessage("Picked textfile:", file.Name + "\r\nReady");
                    txt = await Windows.Storage.FileIO.ReadTextAsync(file);
                    filename = file.Name;
                 
                    var t = Task.Run(async () =>
                    {
                        await sndr.Send(txt, filename);
                    });
                    PostMessage("Picker sending", file.Name);
                }
                else
                {
                    PostMessage("PickAFile", "Operation cancelled.");
                }
            }
            else
            {
                await sndr.Send("Hello World", "Hi.txt");
                PostMessage("Sent:", "Hi.Txt");
            }
            if (sndr != null)
                sndr.Dispose();
        }

//        private async Task SaveAFile(string txt)
//        {
//            bool usedPicker = true;

//            FileSavePicker savePicker = null;
//            Windows.Storage.StorageFile file = null;

//#if IoTCore
//#else
//            savePicker = new Windows.Storage.Pickers.FileSavePicker();
//#endif

//            if (savePicker != null)
//            {
//                savePicker.SuggestedStartLocation =
//                    Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
//                // Dropdown of file types the user can save the file as
//                savePicker.FileTypeChoices.Add("Plain Text", new List<string>() { ".txt" });
//                // Default file name if the user does not type one in or select a file to replace
//                savePicker.SuggestedFileName = "New Document";

//                file = await savePicker.PickSaveFileAsync();
//                if (file == null)
//                {
//                    PostMessage("SaveAFile", "Operation cancelled.");
//                    rcvr = null;
//                    return;
//                }
//            }

//            FileDetail rcvFile = await rcvr.ReadAsync();
//            if (rcvFile == null)
//            {
//                PostMessage("SaveAFile", "Operation failed.");
//                rcvr = null;
//                return;
//            }

//            if (savePicker != null)
//            {              
//                 await file.RenameAsync(rcvFile.filename);
//            }
//            else
//            {
//                usedPicker = false;
//                Windows.Storage.StorageFolder storageFolder =
//                    Windows.Storage.ApplicationData.Current.LocalFolder;
//                file =
//                await storageFolder.CreateFileAsync(rcvFile.filename,
//                    Windows.Storage.CreationCollisionOption.ReplaceExisting);
//            }
//            if (file != null)
//            {
//                // Prevent updates to the remote version of the file until
//                // we finish making changes and call CompleteUpdatesAsync.
//                Windows.Storage.CachedFileManager.DeferUpdates(file);
//                // write to file
//                await Windows.Storage.FileIO.WriteTextAsync(file, rcvFile.txt);
//                // Let Windows know that we're finished changing the file so
//                // the other app can update the remote version of the file.
//                // Completing updates may require Windows to ask for user input.
//                Windows.Storage.Provider.FileUpdateStatus status =
//                    await Windows.Storage.CachedFileManager.CompleteUpdatesAsync(file);
//                if (status == Windows.Storage.Provider.FileUpdateStatus.Complete)
//                {
//                    PostMessage( "File " , file.Name + " was saved in \r\n" + file.Path);
                    
//                }
//                else
//                {
//                    PostMessage( "File " , file.Name + " couldn't be saved.");
//                }
                
//            }

//            rcvr = null;
//        }

        private async Task SaveAFile2(FileDetail rcvFile)
        {

            FileSavePicker savePicker = null;
            Windows.Storage.StorageFile file = null;

            savePicker = new Windows.Storage.Pickers.FileSavePicker();



            savePicker.SuggestedStartLocation =
                Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
            // Dropdown of file types the user can save the file as
            savePicker.FileTypeChoices.Add("Plain Text", new List<string>() { ".txt" });
            // Default file name if the user does not type one in or select a file to replace
            savePicker.SuggestedFileName = rcvFile.filename;

            file = await savePicker.PickSaveFileAsync();
            if (file == null)
            {
                PostMessage("SaveAFile", "Operation cancelled.");
                return;
            }
            

            //await file.RenameAsync(rcvFile.filename);


            // Prevent updates to the remote version of the file until
            // we finish making changes and call CompleteUpdatesAsync.
            Windows.Storage.CachedFileManager.DeferUpdates(file);
            // write to file
            await Windows.Storage.FileIO.WriteTextAsync(file, rcvFile.txt);
            // Let Windows know that we're finished changing the file so
            // the other app can update the remote version of the file.
            // Completing updates may require Windows to ask for user input.
            Windows.Storage.Provider.FileUpdateStatus status =
                await Windows.Storage.CachedFileManager.CompleteUpdatesAsync(file);
            if (status == Windows.Storage.Provider.FileUpdateStatus.Complete)
            {
                PostMessage("File ", file.Name + " was saved in \r\n" + file.Path);

            }
            else
            {
                PostMessage("File ", file.Name + " couldn't be saved.");
            }

        }

        //Windows.Storage.StorageFolder RecvFolder = null;

        //private async Task SaveAFile3(FileDetail rcvFile)
        //{
        //    if (RecvFolder==null)
        //    {
        //        PostMessage("SaveAFile3", "Operation cancelled as no Recv folder.");
        //        return;
        //    }
        //    Windows.Storage.StorageFile file =  await RecvFolder.CreateFileAsync(rcvFile.filename,Windows.Storage.CreationCollisionOption.GenerateUniqueName);
 
        //    if (file == null)
        //    {
        //        PostMessage("SaveAFile3", "Operation cancelled as failed to create file.");
        //        return;
        //    }


        //    // Prevent updates to the remote version of the file until
        //    // we finish making changes and call CompleteUpdatesAsync.
        //    Windows.Storage.CachedFileManager.DeferUpdates(file);
        //    // write to file
        //    await Windows.Storage.FileIO.WriteTextAsync(file, rcvFile.txt);
        //    // Let Windows know that we're finished changing the file so
        //    // the other app can update the remote version of the file.
        //    // Completing updates may require Windows to ask for user input.
        //    Windows.Storage.Provider.FileUpdateStatus status =
        //        await Windows.Storage.CachedFileManager.CompleteUpdatesAsync(file);
        //    if (status == Windows.Storage.Provider.FileUpdateStatus.Complete)
        //    {
        //        PostMessage("File ", file.Name + " was saved in \r\n" + file.Path);

        //    }
        //    else
        //    {
        //        PostMessage("File ", file.Name + " couldn't be saved.");
        //    }

        //}

        internal async void SaveFile(FileDetail rcvFile)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
            {
                await SaveAFile2(rcvFile);
            });
        }

        string txt { get; set; } = "";
        string filename { get; set; } = "";

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            await SendAFile();
        }

        private  void Button_Click_1(object sender, RoutedEventArgs e)
        {
            rcvr.Dispose();
            rcvr = null;
            BtnRecv.Content = "RecvTxt";
        }

        private   void Page_Loaded(object sender, RoutedEventArgs e)
        {



            //PostMessage("", "Ready");
        }

        public bool RecvConnected
        {
            set
            {
                var t = Task.Run(async () =>
                {
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        if (value)
                        {
                            Connected.Text = "Recv Connected";
                            Button_Recv_Disconnect.IsEnabled = true;
                        }
                        else
                        {
                            Connected.Text = "";
                            Button_Recv_Disconnect.IsEnabled = false;
                        }
                    });
                });
            }
        }



        public static MainPage root;

        public void PostMessage(string method,string msg)
        {
            var t = Task.Run(async () =>
                {
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        this.textBlock.Items.Insert(0,string.Format("{0} {1}", method, msg));
                    });
                });
            
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            if (sndr != null)
            {
                sndr.Dispose();
                sndr = null;
            }
            if (rcvr != null)
            {
                rcvr.Dispose();
                rcvr = null;
            }
            Application.Current.Exit();
        }


        string RecvTxt = "";
        private async void Button_Click_3(object sender, RoutedEventArgs e)
        {
            Button butt = (Button)sender;
            if (butt != null)
            {
                string txt = (string)butt.Content;
                if (txt != "Stop Recv")
                {
                    RecvTxt = txt;
                    rcvr = new OBEX_Receiver();
                    await rcvr.Initialize();
                    BtnRecv.Content = "Stop Recv";
                }
                else
                {
                    rcvr.Dispose();
                    rcvr = null;
                    BtnRecv.Content = "RecvTxt";
                    PostMessage("", "Stopped Listening");
                }
            }
        }

        private void AttributeChk_Checked(object sender, RoutedEventArgs e)
        {
            OBEX_Sender.IgnoreAttributeErrors = (AttributeChk.IsChecked == true);
        }
    }
}
