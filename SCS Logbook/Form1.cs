using System;
using System.Drawing;
using System.Globalization;
using System.Drawing.Printing;
using System.IO;
using System.Windows.Forms;
using SCSSdkClient;
using SCSSdkClient.Object;

namespace SCS_Logbook
{
    #region Components legent
    /// <summary>
    /// ************************** COMPONENTS **************************
    /// Txt_Log ==> RichText Box;
    /// menuStrip1 ==> menu holder (Top - Header)
    /// toolStripStatusLabel1 ==> Status bar (bottom)
    /// ***************************** MENU *****************************
    /// menuToolStripMenuItem ==> Header "Menu" button;
    /// settingsToolStripMenuItem ==> Header "Menu -> Setting" Sub-menu button;
    /// SaveLog ==> Header "Menu -> Export" Sub menu button;
    /// printLogToolStripMenuItem ==> Header "Menu -> Print" Sub menu button;
    /// ***************************** EDIT *****************************
    /// editToolStripMenuItem ==> Header "Edit" button;
    /// CopyLog ==> Header "Edit -> Copy Log" Sub menu button;
    ///  </summary>
    #endregion

    public partial class LogBook : Form
    {
        // Telemetry SDK
        private SCSSdkTelemetry Telemetry;
        // Data coming form Telemetry Dll
        private string Amount; // Final revenue from the Job
        private ulong Income; // Initial revenue from the job
        private int XP; // Experience points from the job 
        private string Origin; // Jobs source city 
        private string Destination; // Jobs destination city 
        private string Corp_Origin; // Jobs source company 
        private string Corp_Destination; // Jobs destination company 
        // Printer
        private readonly PrintDocument p_log = new PrintDocument();
        private readonly PrintDialog p_dialog = new PrintDialog();

        #region Telemetry Initialization
        public LogBook()
        {
            InitializeComponent();
            p_log.PrintPage += new PrintPageEventHandler(document_PrintPage);
            Txt_Log.Text = "[" + DateTime.Now.ToString(CultureInfo.CurrentCulture.DateTimeFormat) + "] \nLogbook initialized";
            //CheckForIllegalCrossThreadCalls = false; // Uncomment this line if you get CrossThread Calls Error while debug
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            toolStripStatusLabel1.Text = "Seeking game";
            Telemetry = new SCSSdkTelemetry();

            if (Telemetry.Error != null) return; // Break the code if SDK encounters an error
            SubscribeData(); // If not Subscribe to the Listeners
        }

        private  void SubscribeData()
        { 
            //Automatically refresh values from SDK (1x/sec If sdk is not loaded (game is not running))
            Telemetry.Data += Telemetry_Data;
            Telemetry.JobCancelled += TelemetryJobCancelled;
            Telemetry.JobDelivered += TelemetryJobDelivered;
            Telemetry.JobStarted += TelemetryOnJobStarted;
        }
        #endregion
        
        //***************** Telemetry Functions *****************
        #region Game Data
        private void Telemetry_Data(SCSTelemetry data, bool updated)
        {
            // Use any of the available data provided by the SDK here.
            // Use SCSTelemetry "data" parameter.
            toolStripStatusLabel1.Text = "Game: " + data.Game;

            if (data.SpecialEventsValues.JobDelivered)
            {
                // Sets the Revenue and the XP when "JobDelivered" event occured
                Amount = data.GamePlay.JobDelivered.Revenue.ToString("#0");
                XP = data.GamePlay.JobDelivered.EarnedXp;
            }

            if (data.SpecialEventsValues.JobCancelled)
            {
                // Sets the Loss(money) "JobCancelled" event occured
                Amount = data.GamePlay.JobCancelled.Penalty.ToString("#0");
            }

            if (data.SpecialEventsValues.OnJob)
            {
                // Sets useful information when you have active job (Used for "OnJobStarted" and "JobDelivered" events)
                Origin = data.JobValues.CitySource;
                Destination = data.JobValues.CityDestination;
                Income = data.JobValues.Income;
                Corp_Destination = data.JobValues.CompanyDestination;
                Corp_Origin = data.JobValues.CompanySource;
            }
        }
        #endregion

        #region Game Events
        private void TelemetryOnJobStarted(object sender, EventArgs e) // Action when the corresponding event occurs
        { Txt_Log.Text += "\nJob Started OR loaded: " + Origin + " -> " + Destination + " [$" + Income + "]"; }

        private void TelemetryJobCancelled(object sender, EventArgs e) // Action when the corresponding event occurs
        { Txt_Log.Text += "\nJob Canceled: You paid $" + Amount + " in damages"; }

        private void TelemetryJobDelivered(object sender, EventArgs e) // Action when the corresponding event occurs
        { Txt_Log.Text += "\nJob from " + Corp_Origin + " delivered to " + Corp_Destination + ". \nYou have been paid $" + Amount + " and earned " + XP + "XP"; }

        private void TelemetryFined(object sender, EventArgs e)
        {
            // Do stuff here when Fined
            //Usage: Telemetry.Fined +=TelemetryFined;
            //Should be on "SubscribeData()" Function
        }

        private void TelemetryTollgate(object sender, EventArgs e)
        {
            // Do stuff here when use Tollgates
            //Usage: Telemetry.Tollgate +=TelemetryTollgate;
            //Should be on "SubscribeData()" Function
        }

        private void TelemetryFerry(object sender, EventArgs e)
        {
            // Do stuff here when use Ferry
            //Usage: Telemetry.Ferry +=TelemetryFerry;
            //Should be on "SubscribeData()" Function
        }

        private void TelemetryTrain(object sender, EventArgs e)
        {
            // Do stuff here when use Train
            //Usage: Telemetry.Train +=TelemetryTrain;
            //Should be on "SubscribeData()" Function
        }

        private void TelemetryRefuel(object sender, EventArgs e)
        {
            // Do stuff here when Refuel. (Normally used for "Telemetry.RefuelPayed")
            //Usage: Telemetry.RefuelPayed +=TelemetryRefuel;
            //Should be on "SubscribeData()" Function
        }
        #endregion

        //******************* Menu Functions ********************
        #region Copy Log
        private void CopyLog_click(object sender, EventArgs e)
        {
            if (Txt_Log.Text != string.Empty)
                Clipboard.SetText(Txt_Log.Text);
        }
        #endregion

        #region Print Log

        private void document_PrintPage(object sender, PrintPageEventArgs e)
        {
            e.Graphics.DrawString(Txt_Log.Text, new Font("Arial", 20, FontStyle.Regular), Brushes.Black, 20, 20);
        }
        private void printLogToolStripMenuItem_Click(object sender, EventArgs e)
        {
            p_dialog.Document = p_log;
            if (p_dialog.ShowDialog() == DialogResult.OK)
            {
                p_log.Print();
            }
        }
        #endregion

        #region Save Log
        private void SaveLog_Click(object sender, EventArgs e)
        {
            const string SavePath = "Logs/";
            const string Extension = ".txt";
            var Route = " (" + Origin + "-" + Destination + ") "; // For TripSave variable (cleaner)
            // Vars for the different save system. You can use these directly, but this way keeps code cleaner
            var TripSave = SavePath + DateTime.Now.Date.ToString("MMM-dd-yy") + Route + Extension;
            var UniSave = SavePath + DateTime.Now.Date.ToString("MMM-dd-yy") + Extension;
            
            if (!Directory.Exists(SavePath))// Create "Logs" folder in case is does not exist (deleted)
                Directory.CreateDirectory(SavePath);

            if (LogPerTrip.Checked)
            {
                // If LogPerTrip is enabled, The "JobDelivered" event will trigger this save function
                // And clear the Log text when its done
                File.WriteAllText(TripSave, Txt_Log.Text);
                Txt_Log.Clear();
            }
            else
            {
                // This is the manual Save. You have to press the "Export" button (Or Ctrl+S Hotkey) 
                File.WriteAllText(UniSave, Txt_Log.Text);
            }
        }

        private void LogPerTrip_Click(object sender, EventArgs e)
        {
            if (LogPerTrip.Checked) // If LogPerTrip is enabled the "JobDelivered" event Triggers "SaveLog_Click" function
                Telemetry.JobDelivered += SaveLog_Click;
            else
                Telemetry.JobDelivered -= SaveLog_Click; //Stop Listening When "LogPerTrip" is disabled

        }
        #endregion

    }
}