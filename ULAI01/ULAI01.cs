// ==============================================================================

//  File:                         ULAI01.CS

//  Library Call Demonstrated:    Mccdaq.MccBoard.AIn()

//  Purpose:                      Reads an A/D Input Channel.

//  Demonstration:                Displays the analog input on a user-specified
//                                channel.

//  Other Library Calls:          Mccdaq.MccBoard.ToEngUnits()
//                                MccDaq.MccService.ErrHandling()

//  Special Requirements:         Board 0 must have an A/D converter.
//                                Analog signal on an input channel.

// ==============================================================================

using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using System.Diagnostics;

using AnalogIO;
using MccDaq;
using ErrorDefs;
using System.IO;
using System.Collections.Generic;
using System.Threading;

namespace ULAI01
{
	public class frmDataDisplay : System.Windows.Forms.Form
	{
        BackgroundWorker backworker;
        Stopwatch sw = new Stopwatch(); //Normal Stopwatch
        // Create a new MccBoard object for Board 0
        MccDaq.MccBoard DaqBoard = new MccDaq.MccBoard(0);

        MccDaq.Range Range;
        private int HighChan, NumAIChans;
        private int ADResolution;
        private int DAResolution, NumAOChans; 
        public Label lblDemoFunction;
        public Label lblShowVoltsCh1;
        public Label label2;
        public Label label4;
        public Label label3;
        public Label lblShowVoltsCh2;
        private TextBox txtVoltsToSet;
        private Label label1;
        private Label lblShowVoltage;
        private GroupBox gbx_Setup_Amplifier;
        private TextBox textBox2;
        private Label label6;
        private TextBox textBox1;
        private Label label5;
        private Label lblInitForce;
        private TextBox textBox3;
        private TextBox textBox4;
        private Label label7;
        private TextBox txtDistance;
        private Label label8;
        private TextBox txtForce;
        private Label label9;
        private GroupBox groupBox2;
        private GroupBox groupBox3;
        private CheckBox chkRecordData;
        private Label label10;
        private Label label11;
        public Button btnExportData;
        AnalogIO.clsAnalogIO AIOProps = new AnalogIO.clsAnalogIO();
        public Button btnTest;
        private string File_Name_CSV = "output";
        private bool File_Name_CSV_SET = false;
        private TabControl tabControl1;
        private TabPage tabPage1;
        private TabPage tabPage2;
        private GroupBox gbx_Setup_DAQ;
        private TextBox txt_DAQ_Setup_Name;
        private Label label12;
        private GroupBox gbx_Setup_Display;
        private TextBox txt_DAQ_Setup_Sampling_Rate;
        private Label label13;
        private Label label16;
        private TextBox txt_Display_Sampling;
        private Label label14;
        private Label label15;
        private Label label17;
        private TextBox txt_File_Name;
        private bool Record_Data = false;

        private int Sampling_Rate = 10;
        private int Display_Rate = 10;
        private int Display_Average = 10;
        private TextBox txt_Display_Average;
        private Label label18;
        private float Voltage_ref = 0; 


        //AnalogIO.clsAnalogIO AIOProps = new AnalogIO.clsAnalogIO();
        private List<string> DataDaqList = new List<string>();
        //public MccDaq.ErrorInfo StopBackground(MccDaq.FunctionType funcType);


        private void frmDataDisplay_Load(object eventSender, System.EventArgs eventArgs)
        {

            int LowChan;
            
            MccDaq.TriggerType DefaultTrig;

            InitUL();

            // determine the number of analog channels and their capabilities
            int ChannelType = clsAnalogIO.ANALOGINPUT;
            NumAIChans = AIOProps.FindAnalogChansOfType(DaqBoard, ChannelType,
                out ADResolution, out Range, out LowChan, out DefaultTrig);

            if (NumAIChans == 0)
            {
                lblInstruction.Text = "Board " + DaqBoard.BoardNum.ToString() +
                    " does not have analog input channels.";
                cmdStartConvert.Enabled = false;
                txtNumChan.Enabled = false;
            }
            else
            {
                string CurFunc = "MccBoard.AIn";
                if (ADResolution > 16)
                    CurFunc = "MccBoard.AIn32";
                lblDemoFunction.Text = "Demonstration of " + CurFunc;
                lblInstruction.Text = "Board " + DaqBoard.BoardNum.ToString() +
                    " collecting analog data using " + CurFunc + 
                    " and Range of " + Range.ToString() + ".";
                HighChan = LowChan + NumAIChans - 1;
                this.lblChanPrompt.Text = "Enter a channel (" 
                    + LowChan.ToString() + " - " + HighChan.ToString() + "):";
            }

            ChannelType = clsAnalogIO.ANALOGOUTPUT;
            NumAOChans = AIOProps.FindAnalogChansOfType(DaqBoard, ChannelType,
                out DAResolution, out Range, out LowChan, out DefaultTrig);
            if (NumAOChans == 0)
            {
                lblInstruction.Text = "Board " + DaqBoard.BoardNum.ToString() +
                    " does not have analog input channels.";
                //UpdateButton.Enabled = false;
                //txtVoltsToSet.Enabled = false;
            }
            else
            {
                lblInstruction.Text = "Board " + DaqBoard.BoardNum.ToString() +
                    " generating analog output on channel 0 using cbAOut()" +
                    " and Range of " + Range.ToString() + ".";
                HighChan = LowChan + NumAOChans - 1;
            }

            backworker = new BackgroundWorker();
            backworker.DoWork += new DoWorkEventHandler(backworker_DoWork);
            backworker.ProgressChanged += new ProgressChangedEventHandler
                    (backworker_ProgressChanged);
            backworker.RunWorkerCompleted += new RunWorkerCompletedEventHandler
                    (backworker_RunWorkerCompleted);
            backworker.WorkerReportsProgress = true;
            backworker.WorkerSupportsCancellation = true;
        }

        private void cmdStartConvert_Click(object eventSender, System.EventArgs eventArgs)
        {
            bool IsValidNumber = true;
            float EngUnits = 0.0f;
            int Chan = 0;
            int value;
            SaveFileDialog sfd;
            sfd = new SaveFileDialog();

            sfd.FileName = "DAQ_USB_231" + DateTime.Now.ToString("_yyyy_dd_MM_HH_mm_ss");

            sfd.Filter = "CSV File | *.csv";

            File_Name_CSV = sfd.FileName;

            //DateTime dt;

            using (sfd)
            {
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    //USB_Flags.FileName = sfd.FileName;
                }
                else
                {
                    // TODO: add error message
                    return; 
                }

            }

            if (cmdStartConvert.Text == "Start")
            {
                gbx_Setup_Amplifier.Enabled = false; 
                gbx_Setup_DAQ.Enabled = false;
                gbx_Setup_Display.Enabled = false;

                if (int.TryParse(txt_DAQ_Setup_Sampling_Rate.Text, out value))
                {
                    // x is indeed a valid integer, so value will be equal to 1234.
                    Sampling_Rate = value;
                }
                else
                {
                    // z is NOT a valid integer.
                    // error 
                    MessageBox.Show("Sampling rate not valid", "Setup error", MessageBoxButtons.OKCancel, MessageBoxIcon.Asterisk);
                    return;
                }

                if (int.TryParse(txt_Display_Sampling.Text, out value))
                {
                    // x is indeed a valid integer, so value will be equal to 1234.
                    Display_Rate = value;
                }
                else
                {
                    // z is NOT a valid integer.
                    // error 
                    MessageBox.Show("Display sampling value not valid", "Setup error", MessageBoxButtons.OKCancel, MessageBoxIcon.Asterisk);
                    return;
                }

                if (int.TryParse(txt_Display_Average.Text, out value))
                {
                    // x is indeed a valid integer, so value will be equal to 1234.
                    Display_Average = value;
                }
                else
                {
                    // z is NOT a valid integer.
                    // error 
                    MessageBox.Show("Display Average value not valid", "Setup error", MessageBoxButtons.OKCancel, MessageBoxIcon.Asterisk);
                    return;
                }


                if (Record_Data == true)
                {
                    if (File_Name_CSV_SET == false)
                    {
                        
                    }
                }

                ///////////////////////////////////////////////
                /// Output set
                /// 
                IsValidNumber = float.TryParse(txtVoltsToSet.Text, out EngUnits);

                if (IsValidNumber)
                {
                    //  Parameters:
                    //    Chan       :the D/A output channel
                    //    Range      :ignored if board does not have programmable rage
                    //    DataValue  :the value to send to Chan

                    ushort DataValue = 0;
                    float OutVal;   

                    MccDaq.ErrorInfo ULStat = DaqBoard.FromEngUnits(Range, EngUnits, out DataValue);

                    ULStat = DaqBoard.AOut(Chan, Range, DataValue);

                    //lblValueSent.Text = "The count sent to DAC channel " + Chan.ToString("0") + " was:";
                    //lblVoltage.Text = "The voltage at DAC channel " + Chan.ToString("0") + " is:";
                    //lblShowValue.Text = DataValue.ToString("0");
                    OutVal = ConvertToVolts(DataValue);
                    Voltage_ref = OutVal; 
                    lblShowVoltage.Text = OutVal.ToString("0.0#####") + " Volts";
                }
                //////////////////////////////////////////////////////////////////////////////////////////

                cmdStartConvert.Text = "Stop";
                Start_Backworker(sfd); 
            }
            else
            {
                if (backworker.IsBusy)
                {

                    // Notify the worker thread that a cancel has been requested.
                    // The cancel will not actually happen until the thread in the
                    // DoWork checks the backworker.CancellationPending flag.
                    backworker.CancelAsync();

                    //pgbProgramUSB.Value = 0;
                    //rtbUSBDisplay.AppendText("Cancel Requested, please wait");
                    //rtbUSBDisplay.AppendText(rs); // change line
                    //rtbUSBDisplay.ScrollToCaret(); // get right line position
                }
                cmdStartConvert.Text = "Start";
                gbx_Setup_Amplifier.Enabled = true;
                gbx_Setup_DAQ.Enabled = true;
                gbx_Setup_Display.Enabled = true;
            }
            //if (tmrConvert.Enabled)
            //{
            //    cmdStartConvert.Text = "Start";
            //    tmrConvert.Enabled = false;
            //    ///////////////////////////////////////////////////////////////
            //    //if (backworker.IsBusy)
            //    //{
            //    //    //rtbDisplay.AppendText("Cancel Requested, please wait" + rs);
            //    //    //rtbDisplay.ScrollToCaret();
            //    //    //pgbProgram.Value = 0;
            //    //    // Notify the worker thread that a cancel has been requested.
            //    //    // The cancel will not actually happen until the thread in the
            //    //    // DoWork checks the backworker.CancellationPending flag.
            //    //    backworker.CancelAsync();
            //    //}
            //    ///////////////////////////////////////////////////////////////
            //    chkRecordData.Enabled = true;

            //}
            //else
            //{
            //    cmdStartConvert.Text = "Stop";
            //    //tmrConvert.Enabled = true;
            //    ///////////////////////////////////////////////////////////////
            //    //List<object> arguments = new List<object>();
            //    //arguments.Add(UserCodeFormated); // 0
            //    //arguments.Add(MCU);
            //    //arguments.Add(Source);
            //    //arguments.Add(Destination);
            //    //arguments.Add(flags);

            //    //backworker.RunWorkerAsync(arguments);
            //    //backworker.RunWorkerAsync();

            //    ///////////////////////////////////////////////////////////////
            //    chkRecordData.Enabled = false; 
            //    ///////////////////////////////////////////////
            //    /// Output set
            //    /// 
            //    IsValidNumber = float.TryParse(txtVoltsToSet.Text, out EngUnits);

            //    if (IsValidNumber)
            //    {
            //        //  Parameters:
            //        //    Chan       :the D/A output channel
            //        //    Range      :ignored if board does not have programmable rage
            //        //    DataValue  :the value to send to Chan

            //        ushort DataValue = 0;
            //        float OutVal;

            //        MccDaq.ErrorInfo ULStat = DaqBoard.FromEngUnits(Range, EngUnits, out DataValue);

            //        ULStat = DaqBoard.AOut(Chan, Range, DataValue);

            //        //lblValueSent.Text = "The count sent to DAC channel " + Chan.ToString("0") + " was:";
            //        //lblVoltage.Text = "The voltage at DAC channel " + Chan.ToString("0") + " is:";
            //        //lblShowValue.Text = DataValue.ToString("0");
            //        OutVal = ConvertToVolts(DataValue);
            //        lblShowVoltage.Text = OutVal.ToString("0.0#####") + " Volts";
            //    }
            //    //////////////////////////////////////////////////////////////////////////////////////////
            //}

        }

        private float ConvertToVolts(ushort DataVal)
        {
            float LSBVal, FSVolts, OutVal;

            FSVolts = AIOProps.GetRangeVolts(Range);
            LSBVal = (float)(FSVolts / Math.Pow(2, (double)DAResolution));
            OutVal = LSBVal * DataVal;
            if (Range < Range.Uni10Volts) OutVal = OutVal - (FSVolts / 2);
            return OutVal;
        }

        private void tmrConvert_Tick(object eventSender, System.EventArgs eventArgs)
        {
            //var watch = System.Diagnostics.Stopwatch.StartNew();

            float EngUnits;
            double HighResEngUnits;
            MccDaq.ErrorInfo ULStat;
            System.UInt16 DataValue0;
            System.UInt16 DataValue1;
            System.UInt32 DataValue32;
            int Chan;
            int Options = 0;

            //tmrConvert.Stop();

            //  Collect the data by calling AIn member function of MccBoard object
            //   Parameters:
            //     Chan       :the input channel number
            //     Range      :the Range for the board.
            //     DataValue  :the name for the value collected

            //  set input channel
            //var watch = System.Diagnostics.Stopwatch.StartNew();
            bool ValidChan = int.TryParse(txtNumChan.Text, out Chan);
            if (ValidChan)
            {
                if (Chan > HighChan) Chan = HighChan;
                txtNumChan.Text = Chan.ToString();
            }
            //watch.Stop();
            if (ADResolution > 16)
            {
                //ULStat = DaqBoard.AIn32(Chan, Range, out DataValue32, Options);
                ////  Convert raw data to Volts by calling ToEngUnits
                ////  (member function of MccBoard class)
                //ULStat = DaqBoard.ToEngUnits32(Range, DataValue32, out HighResEngUnits);

                //lblShowData.Text = DataValue32.ToString();                //  print the counts
                //lblShowVoltsCh0.Text = HighResEngUnits.ToString("F5") + " Volts"; //  print the voltage
            }
            else
            {
                //var watch = System.Diagnostics.Stopwatch.StartNew();
                //ULStat = DaqBoard.AIn(0, Range, out DataValue0);
                //watch.Stop();
                ////  Convert raw data to Volts by calling ToEngUnits
                ////  (member function of MccBoard class)
                //ULStat = DaqBoard.ToEngUnits(Range, DataValue0, out EngUnits);

                //lblShowData.Text = DataValue0.ToString();                //  print the counts
                //lblShowVoltsCh0.Text = EngUnits.ToString("F4") + " Volts"; //  print the voltage

                //ConvertVoltForce(DataValue0);

                //////////////////////////////////////////////////////////////////////////////////////
                /// Channel 1
                var watch = System.Diagnostics.Stopwatch.StartNew();
                ULStat = DaqBoard.AIn(1, Range, out DataValue1); // 10ms

                watch.Stop();
                //  Convert raw data to Volts by calling ToEngUnits
                //  (member function of MccBoard class)
                ULStat = DaqBoard.ToEngUnits(Range, DataValue1, out EngUnits);

                lblShowData.Text = DataValue1.ToString();                //  print the counts
                lblShowVoltsCh1.Text = EngUnits.ToString("F4") + " Volts"; //  print the voltage

                ConvertVoltDistance(DataValue1);

                ////////////////////////////////////////////////////////////////////////////////////////
                ///// Channel 2
                //ULStat = DaqBoard.AIn(2, Range, out DataValue2);

                ////  Convert raw data to Volts by calling ToEngUnits
                ////  (member function of MccBoard class)
                //ULStat = DaqBoard.ToEngUnits(Range, DataValue2, out EngUnits);

                //lblShowData.Text = DataValue2.ToString();                //  print the counts
                //lblShowVoltsCh2.Text = EngUnits.ToString("F4") + " Volts"; //  print the voltage

                /////////////////////////////////////////////////////////////////////////////////////////

                if (chkRecordData.Checked == true)
                {
                    //DataDaqList.Add(DateTime.Now.ToString("MM / dd / yyyy hh: mm:ss.fff tt") + "," + DataValue0.ToString() + "," + DataValue1.ToString());
                    //DataDaqList.Add(DateTime.Now.ToString("MM / dd / yyyy hh: mm:ss.fffffff tt") + "," + DataValue0.ToString());
                }
                //DataDaqList.Add(DateTime.Now.ToString() + "," + DataValue0.ToString() + "," + DataValue1.ToString());
                //watch.Stop();

                Console.WriteLine($"Execution Time: {watch.ElapsedMilliseconds} ms");

            }

            //tmrConvert.Start();
        }



        private void cmdStopConvert_Click(object eventSender, System.EventArgs eventArgs)
        {

            //tmrConvert.Enabled = false;
            Application.Exit();

        }

        private void ConvertVoltForce(ushort dataValue)
        {
            ///////////////////////////////////
            /// Value conversion
            /// 

        }

        private void ConvertVoltDistance(ushort dataValue)
        {

        }

        private void InitUL()
        {

            //  Initiate error handling
            //   activating error handling will trap errors like
            //   bad channel numbers and non-configured conditions.
            //   Parameters:
            //     MccDaq.ErrorReporting.PrintAll :all warnings and errors encountered will be printed
            //     MccDaq.ErrorHandling.StopAll   :if an error is encountered, the program will stop

            clsErrorDefs.ReportError = MccDaq.ErrorReporting.PrintAll;
            clsErrorDefs.HandleError = MccDaq.ErrorHandling.StopAll;
            MccDaq.ErrorInfo ULStat = MccDaq.MccService.ErrHandling
                (clsErrorDefs.ReportError, clsErrorDefs.HandleError);

        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////
        ///////////////////////////////////////////////////////////////////////////////////////////////////////
        ///////////////////////////////////////////////////////////////////////////////////////////////////////
        ///////////////////////////////////////////////////////////////////////////////////////////////////////
        void Start_Backworker(SaveFileDialog sfd)
        {
            List<object> arguments = new List<object>();

            arguments.Add(DataDaqList);     // 0
            arguments.Add(File_Name_CSV);   // 1
            arguments.Add(Sampling_Rate);   // 2
            arguments.Add(Display_Rate);    // 3
            arguments.Add(Display_Average); // 4
            arguments.Add(Voltage_ref);     // 5
            arguments.Add(sfd);     // 5
            backworker.RunWorkerAsync(arguments);
        }        
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void backworker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            try
            {
                List<object> genericlist = e.Result as List<object>;
                string result = (string)genericlist[0];

                //pgbProgram.Value = 0;

                //if (e.Cancelled)
                //{
                //    rtbDisplay.AppendText(result + rs);
                //    rtbDisplay.ScrollToCaret();
                //}

                //else if (e.Error != null)
                //{
                //    rtbDisplay.AppendText(result + rs);
                //    rtbDisplay.ScrollToCaret();
                //}
                //else
                //{
                //    rtbDisplay.AppendText(result + rs);
                //    rtbDisplay.ScrollToCaret();
                //}
            }
            catch (Exception ex)
            {
                //Debug.WriteLine("Exception caught for device " + ex.Message);
            }


            //Change the status of the buttons on the UI accordingly
            ////btnStartAsyncOperation.Enabled = true;
            ////btnCancel.Enabled = false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void backworker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            //pgbProgram.Value = e.ProgressPercentage;
            //rtbDisplay.AppendText(e.UserState + rs);
            //rtbDisplay.ScrollToCaret();
            
        }

        ////
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void backworker_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            List<object> genericlist = e.Argument as List<object>;
            List<string> dataDaqList_thread = (List<string>)genericlist[0]; // 0
            string file_name_thread = (string)genericlist[1];               // 1
            int rate_thread = (int)genericlist[2];                          // 2
            int display_sampling_thread = (int)genericlist[3];              // 3
            int display_average_thread = (int)genericlist[4];               // 4
            float voltage_ref_thread = (float)genericlist[5];               // 5
            SaveFileDialog sfd_tread = (SaveFileDialog)genericlist[6];      // 6
            ///////////////////////////////////////////////////////////////////////////////
            //int CounterType = Counters.clsCounters.CTRSCAN;
            int NumCtrs = 0;
            int CounterNum = 1; 
            int numPoints = 1024;    //  Number of data points to collect
            int FirstPoint = 0;     //  set first element in buffer to transfer to array
            int LastCtr;
            uint[] ADCData;             //  dimension an array to hold the input values
            IntPtr memHandle = IntPtr.Zero;
            int CurIndex;
            int CurCount;
            short Status;
            //Label lblInstruction = ;	//  define a variable to contain the handle for memory allocated 
            //int rate = 25000;
            float EngUnits;
            //double HighResEngUnits;
            //MccDaq.ErrorInfo ULStat;
            uint dataValue;
            // Allocate memory buffer to hold data..
            ADCData = new uint[numPoints];
            memHandle = MccDaq.MccService.WinBufAlloc32Ex(numPoints); //  set aside memory to hold data
            //lblInstruction.Text = "Board " + DaqBoard.BoardNum.ToString() +
            //    " collecting counter data on up to " + NumCtrs.ToString() +
            //    " channels using CInScan.";
            LastCtr = CounterNum + NumCtrs - 1;
            //this.txtLastCtr.Text = LastCtr.ToString();
            //this.lblFirstCtr.Text = "Measure Counter " + CounterNum.ToString() + " to";

            MccDaq.ErrorInfo ULStat;
            MccDaq.ScanOptions options;
            MccDaq.Range range;
            range = MccDaq.Range.Bip10Volts;
            options = MccDaq.ScanOptions.Continuous | MccDaq.ScanOptions.Background;
            
            //MccDaq.FunctionType function;
            //function = MccDaq.FunctionType.AiFunction;


            ULStat = DaqBoard.AInScan(0, 0, numPoints, ref rate_thread, range, memHandle, options);

            ULStat = DaqBoard.GetStatus(out Status, out CurCount, out CurIndex, MccDaq.FunctionType.AiFunction);

            if(Status == MccDaq.MccBoard.Running)
            {
                worker.ReportProgress(1, "Background command started");
            }
            else
            {
                worker.ReportProgress(1, "Background command broken");
            }

            //StreamWriter sw = new StreamWriter(file_name_thread + ".csv");
            //StreamWriter sw = File.CreateText(file_name_thread + ".csv"); 
            using (StreamWriter sw = File.CreateText(sfd_tread.FileName))
            {
                sw.WriteLine(DateTime.Now.ToString("MM / dd / yyyy hh: mm:ss.fffffff tt"));
                sw.WriteLine("Reference voltage = " + voltage_ref_thread + " Volts");
                sw.WriteLine("Time, Data ADC, Ref offset");

                while (true)
                {
                    //Thread.Sleep(1); 

                    ULStat = DaqBoard.GetStatus(out Status, out CurCount, out CurIndex, MccDaq.FunctionType.AiFunction);

                    if (CurCount > 63) // half size buffer
                    {
                        //ULStat = MccDaq.MccService.WinBufToArray32(memHandle, ADCData, FirstPoint, numPoints);
                        // ULStat = MccDaq.MccService.WinBufToArray32(memHandle, ADCData, FirstPoint, ADCData.Length);
                        ULStat = MccDaq.MccService.WinBufToArray32(memHandle, ADCData, FirstPoint, 128);

                        //string time_last_sample = DateTime.Now.ToString("MM / dd / yyyy hh: mm:ss.fffffff tt");
                        string time_last_sample = DateTime.Now.ToString("mm:ss.fffffff tt");
                        for (int i = 0; i < 128; i++) // ADCData.Length
                        {
                            //dataDaqList_thread.Add(DateTime.Now.ToString("MM / dd / yyyy hh: mm:ss.fffffff tt") + "," + ADCData[i].ToString());

                            dataValue = ADCData[i];

                            ULStat = DaqBoard.ToEngUnits(Range, (Int16)dataValue, out EngUnits);

                            //dataDaqList_thread.Add(time_last_sample + "," + ADCData[i].ToString());

                            //dataDaqList_thread.Add(time_last_sample + "," + EngUnits.ToString() + "," + voltage_ref_thread);

                            if(i<127)
                            {
                                //sw.WriteLine(i + "," + EngUnits.ToString() + "," + voltage_ref_thread);
                                sw.WriteLine(i + "," + EngUnits.ToString());
                            }
                            else
                            {
                                //sw.WriteLine(time_last_sample + "," + EngUnits.ToString() + "," + voltage_ref_thread);
                                sw.WriteLine(time_last_sample + "," + EngUnits.ToString());
                            }
                            

                        }
                    }
                    else
                    {
                        //if(dataDaqList_thread.Count > 0)
                        //{
                        //    try
                        //    {
                        //        for(int i = 0; i< dataDaqList_thread.Count;i++)
                        //        {

                        //            sw.WriteLine(dataDaqList_thread[i]);
                        //        }

                        //        dataDaqList_thread.Clear(); 
                        //    }
                        //    catch
                        //    {
                        //        // error 
                        //    }
                        //}
                    }



                    if (worker.CancellationPending)
                    {
                        //int StopBackground(0, 0); // stop continuous
                        //MccDaq.MccBoard.StopBackground(function);

                        ULStat = DaqBoard.StopBackground(MccDaq.FunctionType.AiFunction);
                        e.Result = BackgroundWorkerReportComplete("Cancel");
                        sw.Close();
                        return;
                    }
                }
            }
        }

        private object BackgroundWorkerReportComplete(string game_over)
        {
            List<object> arguments_Back_out = new List<object>();



            arguments_Back_out.Add(game_over);

            return arguments_Back_out;
        }



        ///////////////////////////////////////////////////////////////////////////////////////////////////////
        ///////////////////////////////////////////////////////////////////////////////////////////////////////
        ///////////////////////////////////////////////////////////////////////////////////////////////////////
        ///////////////////////////////////////////////////////////////////////////////////////////////////////

        #region Windows Form Designer generated code
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>

        private void InitializeComponent()
		{
            this.components = new System.ComponentModel.Container();
            this.ToolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.cmdStartConvert = new System.Windows.Forms.Button();
            this.cmdStopConvert = new System.Windows.Forms.Button();
            this.txtNumChan = new System.Windows.Forms.TextBox();
            this.lblShowVoltsCh0 = new System.Windows.Forms.Label();
            this.lblVoltsRead = new System.Windows.Forms.Label();
            this.lblValueRead = new System.Windows.Forms.Label();
            this.lblChanPrompt = new System.Windows.Forms.Label();
            this.lblShowData = new System.Windows.Forms.Label();
            this.lblInstruction = new System.Windows.Forms.Label();
            this.lblDemoFunction = new System.Windows.Forms.Label();
            this.lblShowVoltsCh1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.lblShowVoltsCh2 = new System.Windows.Forms.Label();
            this.txtVoltsToSet = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.lblShowVoltage = new System.Windows.Forms.Label();
            this.gbx_Setup_Amplifier = new System.Windows.Forms.GroupBox();
            this.chkRecordData = new System.Windows.Forms.CheckBox();
            this.textBox3 = new System.Windows.Forms.TextBox();
            this.textBox4 = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.textBox2 = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.lblInitForce = new System.Windows.Forms.Label();
            this.txtDistance = new System.Windows.Forms.TextBox();
            this.label8 = new System.Windows.Forms.Label();
            this.txtForce = new System.Windows.Forms.TextBox();
            this.label9 = new System.Windows.Forms.Label();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.label11 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.btnExportData = new System.Windows.Forms.Button();
            this.btnTest = new System.Windows.Forms.Button();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.gbx_Setup_DAQ = new System.Windows.Forms.GroupBox();
            this.label12 = new System.Windows.Forms.Label();
            this.txt_DAQ_Setup_Name = new System.Windows.Forms.TextBox();
            this.txt_DAQ_Setup_Sampling_Rate = new System.Windows.Forms.TextBox();
            this.label13 = new System.Windows.Forms.Label();
            this.gbx_Setup_Display = new System.Windows.Forms.GroupBox();
            this.txt_Display_Sampling = new System.Windows.Forms.TextBox();
            this.label14 = new System.Windows.Forms.Label();
            this.label15 = new System.Windows.Forms.Label();
            this.label16 = new System.Windows.Forms.Label();
            this.txt_File_Name = new System.Windows.Forms.TextBox();
            this.label17 = new System.Windows.Forms.Label();
            this.txt_Display_Average = new System.Windows.Forms.TextBox();
            this.label18 = new System.Windows.Forms.Label();
            this.gbx_Setup_Amplifier.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.gbx_Setup_DAQ.SuspendLayout();
            this.gbx_Setup_Display.SuspendLayout();
            this.SuspendLayout();
            // 
            // cmdStartConvert
            // 
            this.cmdStartConvert.BackColor = System.Drawing.SystemColors.Control;
            this.cmdStartConvert.Cursor = System.Windows.Forms.Cursors.Default;
            this.cmdStartConvert.Font = new System.Drawing.Font("Arial", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cmdStartConvert.ForeColor = System.Drawing.SystemColors.ControlText;
            this.cmdStartConvert.Location = new System.Drawing.Point(141, 405);
            this.cmdStartConvert.Name = "cmdStartConvert";
            this.cmdStartConvert.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.cmdStartConvert.Size = new System.Drawing.Size(52, 26);
            this.cmdStartConvert.TabIndex = 5;
            this.cmdStartConvert.Text = "Start";
            this.cmdStartConvert.UseVisualStyleBackColor = false;
            this.cmdStartConvert.Click += new System.EventHandler(this.cmdStartConvert_Click);
            // 
            // cmdStopConvert
            // 
            this.cmdStopConvert.BackColor = System.Drawing.SystemColors.Control;
            this.cmdStopConvert.Cursor = System.Windows.Forms.Cursors.Default;
            this.cmdStopConvert.Font = new System.Drawing.Font("Arial", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cmdStopConvert.ForeColor = System.Drawing.SystemColors.ControlText;
            this.cmdStopConvert.Location = new System.Drawing.Point(303, 405);
            this.cmdStopConvert.Name = "cmdStopConvert";
            this.cmdStopConvert.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.cmdStopConvert.Size = new System.Drawing.Size(52, 26);
            this.cmdStopConvert.TabIndex = 6;
            this.cmdStopConvert.Text = "Quit";
            this.cmdStopConvert.UseVisualStyleBackColor = false;
            this.cmdStopConvert.Click += new System.EventHandler(this.cmdStopConvert_Click);
            // 
            // txtNumChan
            // 
            this.txtNumChan.AcceptsReturn = true;
            this.txtNumChan.BackColor = System.Drawing.SystemColors.Window;
            this.txtNumChan.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtNumChan.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.txtNumChan.Font = new System.Drawing.Font("Arial", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtNumChan.ForeColor = System.Drawing.SystemColors.WindowText;
            this.txtNumChan.Location = new System.Drawing.Point(1087, 224);
            this.txtNumChan.MaxLength = 0;
            this.txtNumChan.Name = "txtNumChan";
            this.txtNumChan.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.txtNumChan.Size = new System.Drawing.Size(33, 20);
            this.txtNumChan.TabIndex = 0;
            this.txtNumChan.Text = "0";
            this.txtNumChan.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.txtNumChan.Visible = false;
            this.txtNumChan.TextChanged += new System.EventHandler(this.txtNumChan_TextChanged);
            // 
            // lblShowVoltsCh0
            // 
            this.lblShowVoltsCh0.BackColor = System.Drawing.SystemColors.Window;
            this.lblShowVoltsCh0.Cursor = System.Windows.Forms.Cursors.Default;
            this.lblShowVoltsCh0.Font = new System.Drawing.Font("Arial", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblShowVoltsCh0.ForeColor = System.Drawing.Color.Blue;
            this.lblShowVoltsCh0.Location = new System.Drawing.Point(154, 27);
            this.lblShowVoltsCh0.Name = "lblShowVoltsCh0";
            this.lblShowVoltsCh0.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.lblShowVoltsCh0.Size = new System.Drawing.Size(80, 16);
            this.lblShowVoltsCh0.TabIndex = 8;
            this.lblShowVoltsCh0.Text = "value";
            this.lblShowVoltsCh0.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblVoltsRead
            // 
            this.lblVoltsRead.BackColor = System.Drawing.SystemColors.Window;
            this.lblVoltsRead.Cursor = System.Windows.Forms.Cursors.Default;
            this.lblVoltsRead.Font = new System.Drawing.Font("Arial", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblVoltsRead.ForeColor = System.Drawing.SystemColors.WindowText;
            this.lblVoltsRead.Location = new System.Drawing.Point(23, 27);
            this.lblVoltsRead.Name = "lblVoltsRead";
            this.lblVoltsRead.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.lblVoltsRead.Size = new System.Drawing.Size(125, 16);
            this.lblVoltsRead.TabIndex = 7;
            this.lblVoltsRead.Text = "Value voltage channel 0:";
            this.lblVoltsRead.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblValueRead
            // 
            this.lblValueRead.BackColor = System.Drawing.SystemColors.Window;
            this.lblValueRead.Cursor = System.Windows.Forms.Cursors.Default;
            this.lblValueRead.Font = new System.Drawing.Font("Arial", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblValueRead.ForeColor = System.Drawing.SystemColors.WindowText;
            this.lblValueRead.Location = new System.Drawing.Point(906, 282);
            this.lblValueRead.Name = "lblValueRead";
            this.lblValueRead.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.lblValueRead.Size = new System.Drawing.Size(184, 16);
            this.lblValueRead.TabIndex = 3;
            this.lblValueRead.Text = "Value read from selected channel:";
            this.lblValueRead.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.lblValueRead.Visible = false;
            // 
            // lblChanPrompt
            // 
            this.lblChanPrompt.BackColor = System.Drawing.SystemColors.Window;
            this.lblChanPrompt.Cursor = System.Windows.Forms.Cursors.Default;
            this.lblChanPrompt.Font = new System.Drawing.Font("Arial", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblChanPrompt.ForeColor = System.Drawing.SystemColors.WindowText;
            this.lblChanPrompt.Location = new System.Drawing.Point(850, 225);
            this.lblChanPrompt.Name = "lblChanPrompt";
            this.lblChanPrompt.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.lblChanPrompt.Size = new System.Drawing.Size(217, 16);
            this.lblChanPrompt.TabIndex = 1;
            this.lblChanPrompt.Text = "Enter the Channel to display: ";
            this.lblChanPrompt.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.lblChanPrompt.Visible = false;
            // 
            // lblShowData
            // 
            this.lblShowData.Font = new System.Drawing.Font("Arial", 8F);
            this.lblShowData.ForeColor = System.Drawing.Color.Blue;
            this.lblShowData.Location = new System.Drawing.Point(919, 312);
            this.lblShowData.Name = "lblShowData";
            this.lblShowData.Size = new System.Drawing.Size(80, 16);
            this.lblShowData.TabIndex = 9;
            this.lblShowData.Text = "digital";
            this.lblShowData.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblShowData.Visible = false;
            // 
            // lblInstruction
            // 
            this.lblInstruction.BackColor = System.Drawing.SystemColors.Window;
            this.lblInstruction.Cursor = System.Windows.Forms.Cursors.Default;
            this.lblInstruction.Font = new System.Drawing.Font("Arial", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblInstruction.ForeColor = System.Drawing.Color.Red;
            this.lblInstruction.Location = new System.Drawing.Point(834, 123);
            this.lblInstruction.Name = "lblInstruction";
            this.lblInstruction.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.lblInstruction.Size = new System.Drawing.Size(331, 75);
            this.lblInstruction.TabIndex = 10;
            this.lblInstruction.Text = "Demonstration of MccBoard.AIn";
            this.lblInstruction.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // lblDemoFunction
            // 
            this.lblDemoFunction.BackColor = System.Drawing.SystemColors.Window;
            this.lblDemoFunction.Cursor = System.Windows.Forms.Cursors.Default;
            this.lblDemoFunction.Font = new System.Drawing.Font("Arial", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblDemoFunction.ForeColor = System.Drawing.SystemColors.WindowText;
            this.lblDemoFunction.Location = new System.Drawing.Point(841, 71);
            this.lblDemoFunction.Name = "lblDemoFunction";
            this.lblDemoFunction.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.lblDemoFunction.Size = new System.Drawing.Size(288, 74);
            this.lblDemoFunction.TabIndex = 2;
            this.lblDemoFunction.Text = "Demonstration of MccBoard.AIn";
            this.lblDemoFunction.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // lblShowVoltsCh1
            // 
            this.lblShowVoltsCh1.BackColor = System.Drawing.SystemColors.Window;
            this.lblShowVoltsCh1.Cursor = System.Windows.Forms.Cursors.Default;
            this.lblShowVoltsCh1.Font = new System.Drawing.Font("Arial", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblShowVoltsCh1.ForeColor = System.Drawing.Color.Blue;
            this.lblShowVoltsCh1.Location = new System.Drawing.Point(154, 56);
            this.lblShowVoltsCh1.Name = "lblShowVoltsCh1";
            this.lblShowVoltsCh1.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.lblShowVoltsCh1.Size = new System.Drawing.Size(80, 16);
            this.lblShowVoltsCh1.TabIndex = 12;
            this.lblShowVoltsCh1.Text = "value";
            this.lblShowVoltsCh1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label2
            // 
            this.label2.BackColor = System.Drawing.SystemColors.Window;
            this.label2.Cursor = System.Windows.Forms.Cursors.Default;
            this.label2.Font = new System.Drawing.Font("Arial", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.ForeColor = System.Drawing.SystemColors.WindowText;
            this.label2.Location = new System.Drawing.Point(20, 56);
            this.label2.Name = "label2";
            this.label2.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.label2.Size = new System.Drawing.Size(128, 16);
            this.label2.TabIndex = 11;
            this.label2.Text = "Value voltage channel 1:";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label4
            // 
            this.label4.BackColor = System.Drawing.SystemColors.Window;
            this.label4.Cursor = System.Windows.Forms.Cursors.Default;
            this.label4.Font = new System.Drawing.Font("Arial", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.ForeColor = System.Drawing.SystemColors.WindowText;
            this.label4.Location = new System.Drawing.Point(20, 85);
            this.label4.Name = "label4";
            this.label4.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.label4.Size = new System.Drawing.Size(128, 16);
            this.label4.TabIndex = 13;
            this.label4.Text = "Value voltage channel 2:";
            this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label3
            // 
            this.label3.BackColor = System.Drawing.SystemColors.Window;
            this.label3.Cursor = System.Windows.Forms.Cursors.Default;
            this.label3.Font = new System.Drawing.Font("Arial", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.ForeColor = System.Drawing.SystemColors.WindowText;
            this.label3.Location = new System.Drawing.Point(20, 114);
            this.label3.Name = "label3";
            this.label3.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.label3.Size = new System.Drawing.Size(113, 16);
            this.label3.TabIndex = 14;
            this.label3.Text = "Value voltage Output:";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblShowVoltsCh2
            // 
            this.lblShowVoltsCh2.BackColor = System.Drawing.SystemColors.Window;
            this.lblShowVoltsCh2.Cursor = System.Windows.Forms.Cursors.Default;
            this.lblShowVoltsCh2.Font = new System.Drawing.Font("Arial", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblShowVoltsCh2.ForeColor = System.Drawing.Color.Blue;
            this.lblShowVoltsCh2.Location = new System.Drawing.Point(154, 85);
            this.lblShowVoltsCh2.Name = "lblShowVoltsCh2";
            this.lblShowVoltsCh2.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.lblShowVoltsCh2.Size = new System.Drawing.Size(80, 16);
            this.lblShowVoltsCh2.TabIndex = 15;
            this.lblShowVoltsCh2.Text = "value";
            this.lblShowVoltsCh2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtVoltsToSet
            // 
            this.txtVoltsToSet.Font = new System.Drawing.Font("Arial", 8.25F);
            this.txtVoltsToSet.Location = new System.Drawing.Point(157, 113);
            this.txtVoltsToSet.Name = "txtVoltsToSet";
            this.txtVoltsToSet.Size = new System.Drawing.Size(58, 20);
            this.txtVoltsToSet.TabIndex = 16;
            this.txtVoltsToSet.Text = "0";
            this.txtVoltsToSet.TextChanged += new System.EventHandler(this.txtVoltsToSet_TextChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Arial", 8.25F);
            this.label1.Location = new System.Drawing.Point(221, 116);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(31, 14);
            this.label1.TabIndex = 17;
            this.label1.Text = "Volts";
            // 
            // lblShowVoltage
            // 
            this.lblShowVoltage.AutoSize = true;
            this.lblShowVoltage.Font = new System.Drawing.Font("Arial", 8.25F);
            this.lblShowVoltage.Location = new System.Drawing.Point(267, 116);
            this.lblShowVoltage.Name = "lblShowVoltage";
            this.lblShowVoltage.Size = new System.Drawing.Size(0, 14);
            this.lblShowVoltage.TabIndex = 18;
            // 
            // gbx_Setup_Amplifier
            // 
            this.gbx_Setup_Amplifier.Controls.Add(this.textBox3);
            this.gbx_Setup_Amplifier.Controls.Add(this.textBox4);
            this.gbx_Setup_Amplifier.Controls.Add(this.label7);
            this.gbx_Setup_Amplifier.Controls.Add(this.textBox2);
            this.gbx_Setup_Amplifier.Controls.Add(this.label6);
            this.gbx_Setup_Amplifier.Controls.Add(this.textBox1);
            this.gbx_Setup_Amplifier.Controls.Add(this.label5);
            this.gbx_Setup_Amplifier.Controls.Add(this.lblInitForce);
            this.gbx_Setup_Amplifier.Location = new System.Drawing.Point(43, 27);
            this.gbx_Setup_Amplifier.Name = "gbx_Setup_Amplifier";
            this.gbx_Setup_Amplifier.Size = new System.Drawing.Size(332, 137);
            this.gbx_Setup_Amplifier.TabIndex = 19;
            this.gbx_Setup_Amplifier.TabStop = false;
            this.gbx_Setup_Amplifier.Text = "Setup Amplifier";
            // 
            // chkRecordData
            // 
            this.chkRecordData.AutoSize = true;
            this.chkRecordData.Font = new System.Drawing.Font("Arial", 8.25F);
            this.chkRecordData.Location = new System.Drawing.Point(26, 151);
            this.chkRecordData.Name = "chkRecordData";
            this.chkRecordData.Size = new System.Drawing.Size(112, 18);
            this.chkRecordData.TabIndex = 8;
            this.chkRecordData.Text = "Record Raw Data";
            this.chkRecordData.UseVisualStyleBackColor = true;
            this.chkRecordData.CheckedChanged += new System.EventHandler(this.chkRecordData_CheckedChanged);
            // 
            // textBox3
            // 
            this.textBox3.Enabled = false;
            this.textBox3.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Bold);
            this.textBox3.Location = new System.Drawing.Point(191, 88);
            this.textBox3.Name = "textBox3";
            this.textBox3.Size = new System.Drawing.Size(67, 20);
            this.textBox3.TabIndex = 7;
            this.textBox3.Text = "1";
            this.textBox3.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // textBox4
            // 
            this.textBox4.Enabled = false;
            this.textBox4.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Bold);
            this.textBox4.Location = new System.Drawing.Point(191, 51);
            this.textBox4.Name = "textBox4";
            this.textBox4.Size = new System.Drawing.Size(67, 20);
            this.textBox4.TabIndex = 6;
            this.textBox4.Text = "50";
            this.textBox4.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Bold);
            this.label7.Location = new System.Drawing.Point(211, 32);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(31, 14);
            this.label7.TabIndex = 5;
            this.label7.Text = "Gain";
            // 
            // textBox2
            // 
            this.textBox2.Enabled = false;
            this.textBox2.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Bold);
            this.textBox2.Location = new System.Drawing.Point(107, 88);
            this.textBox2.Name = "textBox2";
            this.textBox2.Size = new System.Drawing.Size(67, 20);
            this.textBox2.TabIndex = 4;
            this.textBox2.Text = "1";
            this.textBox2.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Bold);
            this.label6.Location = new System.Drawing.Point(43, 91);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(66, 14);
            this.label6.TabIndex = 3;
            this.label6.Text = "Distance = ";
            // 
            // textBox1
            // 
            this.textBox1.Enabled = false;
            this.textBox1.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Bold);
            this.textBox1.Location = new System.Drawing.Point(107, 51);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(67, 20);
            this.textBox1.TabIndex = 2;
            this.textBox1.Text = "0";
            this.textBox1.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Bold);
            this.label5.Location = new System.Drawing.Point(104, 32);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(78, 14);
            this.label5.TabIndex = 1;
            this.label5.Text = "Channel ADC";
            // 
            // lblInitForce
            // 
            this.lblInitForce.AutoSize = true;
            this.lblInitForce.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Bold);
            this.lblInitForce.Location = new System.Drawing.Point(43, 54);
            this.lblInitForce.Name = "lblInitForce";
            this.lblInitForce.Size = new System.Drawing.Size(50, 14);
            this.lblInitForce.TabIndex = 0;
            this.lblInitForce.Text = "Force = ";
            // 
            // txtDistance
            // 
            this.txtDistance.Enabled = false;
            this.txtDistance.Font = new System.Drawing.Font("Arial", 8.25F);
            this.txtDistance.Location = new System.Drawing.Point(81, 59);
            this.txtDistance.Name = "txtDistance";
            this.txtDistance.Size = new System.Drawing.Size(67, 20);
            this.txtDistance.TabIndex = 11;
            this.txtDistance.Text = "1";
            this.txtDistance.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Font = new System.Drawing.Font("Arial", 8.25F);
            this.label8.Location = new System.Drawing.Point(17, 62);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(61, 14);
            this.label8.TabIndex = 10;
            this.label8.Text = "Distance = ";
            // 
            // txtForce
            // 
            this.txtForce.Enabled = false;
            this.txtForce.Font = new System.Drawing.Font("Arial", 8.25F);
            this.txtForce.Location = new System.Drawing.Point(81, 33);
            this.txtForce.Name = "txtForce";
            this.txtForce.Size = new System.Drawing.Size(67, 20);
            this.txtForce.TabIndex = 9;
            this.txtForce.Text = "0";
            this.txtForce.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Font = new System.Drawing.Font("Arial", 8.25F);
            this.label9.Location = new System.Drawing.Point(17, 36);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(47, 14);
            this.label9.TabIndex = 8;
            this.label9.Text = "Force = ";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.label17);
            this.groupBox2.Controls.Add(this.txt_File_Name);
            this.groupBox2.Controls.Add(this.chkRecordData);
            this.groupBox2.Controls.Add(this.lblVoltsRead);
            this.groupBox2.Controls.Add(this.lblShowVoltsCh0);
            this.groupBox2.Controls.Add(this.label2);
            this.groupBox2.Controls.Add(this.lblShowVoltsCh1);
            this.groupBox2.Controls.Add(this.lblShowVoltage);
            this.groupBox2.Controls.Add(this.label4);
            this.groupBox2.Controls.Add(this.label3);
            this.groupBox2.Controls.Add(this.lblShowVoltsCh2);
            this.groupBox2.Controls.Add(this.label1);
            this.groupBox2.Controls.Add(this.txtVoltsToSet);
            this.groupBox2.Location = new System.Drawing.Point(45, 28);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(332, 238);
            this.groupBox2.TabIndex = 20;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Raw Value - Debug";
            this.groupBox2.Enter += new System.EventHandler(this.groupBox2_Enter);
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.label11);
            this.groupBox3.Controls.Add(this.label10);
            this.groupBox3.Controls.Add(this.label9);
            this.groupBox3.Controls.Add(this.txtForce);
            this.groupBox3.Controls.Add(this.txtDistance);
            this.groupBox3.Controls.Add(this.label8);
            this.groupBox3.Location = new System.Drawing.Point(45, 284);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(332, 100);
            this.groupBox3.TabIndex = 21;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Output";
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Font = new System.Drawing.Font("Arial", 8.25F);
            this.label11.Location = new System.Drawing.Point(154, 62);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(39, 14);
            this.label11.TabIndex = 13;
            this.label11.Text = "Inches";
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Font = new System.Drawing.Font("Arial", 8.25F);
            this.label10.Location = new System.Drawing.Point(154, 36);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(25, 14);
            this.label10.TabIndex = 12;
            this.label10.Text = "Lbs";
            // 
            // btnExportData
            // 
            this.btnExportData.BackColor = System.Drawing.SystemColors.Control;
            this.btnExportData.Cursor = System.Windows.Forms.Cursors.Default;
            this.btnExportData.Font = new System.Drawing.Font("Arial", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnExportData.ForeColor = System.Drawing.SystemColors.ControlText;
            this.btnExportData.Location = new System.Drawing.Point(210, 405);
            this.btnExportData.Name = "btnExportData";
            this.btnExportData.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.btnExportData.Size = new System.Drawing.Size(52, 26);
            this.btnExportData.TabIndex = 22;
            this.btnExportData.Text = "Export";
            this.btnExportData.UseVisualStyleBackColor = false;
            this.btnExportData.Click += new System.EventHandler(this.btnExportData_Click);
            // 
            // btnTest
            // 
            this.btnTest.BackColor = System.Drawing.SystemColors.Control;
            this.btnTest.Cursor = System.Windows.Forms.Cursors.Default;
            this.btnTest.Font = new System.Drawing.Font("Arial", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnTest.ForeColor = System.Drawing.SystemColors.ControlText;
            this.btnTest.Location = new System.Drawing.Point(45, 405);
            this.btnTest.Name = "btnTest";
            this.btnTest.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.btnTest.Size = new System.Drawing.Size(52, 26);
            this.btnTest.TabIndex = 23;
            this.btnTest.Text = "Test";
            this.btnTest.UseVisualStyleBackColor = false;
            this.btnTest.Click += new System.EventHandler(this.btnTest_Click);
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Location = new System.Drawing.Point(1, 1);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(436, 508);
            this.tabControl1.TabIndex = 24;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.groupBox3);
            this.tabPage1.Controls.Add(this.groupBox2);
            this.tabPage1.Controls.Add(this.btnTest);
            this.tabPage1.Controls.Add(this.btnExportData);
            this.tabPage1.Controls.Add(this.cmdStopConvert);
            this.tabPage1.Controls.Add(this.cmdStartConvert);
            this.tabPage1.Location = new System.Drawing.Point(4, 23);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(428, 481);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Measurement";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.gbx_Setup_Display);
            this.tabPage2.Controls.Add(this.gbx_Setup_DAQ);
            this.tabPage2.Controls.Add(this.gbx_Setup_Amplifier);
            this.tabPage2.Location = new System.Drawing.Point(4, 23);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(428, 481);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Setup";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // gbx_Setup_DAQ
            // 
            this.gbx_Setup_DAQ.Controls.Add(this.label15);
            this.gbx_Setup_DAQ.Controls.Add(this.txt_DAQ_Setup_Sampling_Rate);
            this.gbx_Setup_DAQ.Controls.Add(this.label13);
            this.gbx_Setup_DAQ.Controls.Add(this.txt_DAQ_Setup_Name);
            this.gbx_Setup_DAQ.Controls.Add(this.label12);
            this.gbx_Setup_DAQ.Location = new System.Drawing.Point(43, 189);
            this.gbx_Setup_DAQ.Name = "gbx_Setup_DAQ";
            this.gbx_Setup_DAQ.Size = new System.Drawing.Size(332, 123);
            this.gbx_Setup_DAQ.TabIndex = 20;
            this.gbx_Setup_DAQ.TabStop = false;
            this.gbx_Setup_DAQ.Text = "Setup DAQ";
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(31, 32);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(89, 14);
            this.label12.TabIndex = 0;
            this.label12.Text = "Device name = ";
            // 
            // txt_DAQ_Setup_Name
            // 
            this.txt_DAQ_Setup_Name.Location = new System.Drawing.Point(126, 29);
            this.txt_DAQ_Setup_Name.Name = "txt_DAQ_Setup_Name";
            this.txt_DAQ_Setup_Name.Size = new System.Drawing.Size(184, 20);
            this.txt_DAQ_Setup_Name.TabIndex = 1;
            this.txt_DAQ_Setup_Name.Text = "USB-231";
            this.txt_DAQ_Setup_Name.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // txt_DAQ_Setup_Sampling_Rate
            // 
            this.txt_DAQ_Setup_Sampling_Rate.Location = new System.Drawing.Point(126, 70);
            this.txt_DAQ_Setup_Sampling_Rate.Name = "txt_DAQ_Setup_Sampling_Rate";
            this.txt_DAQ_Setup_Sampling_Rate.Size = new System.Drawing.Size(75, 20);
            this.txt_DAQ_Setup_Sampling_Rate.TabIndex = 3;
            this.txt_DAQ_Setup_Sampling_Rate.Text = "50000";
            this.txt_DAQ_Setup_Sampling_Rate.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(23, 73);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(97, 14);
            this.label13.TabIndex = 2;
            this.label13.Text = "Sampling Rate = ";
            // 
            // gbx_Setup_Display
            // 
            this.gbx_Setup_Display.Controls.Add(this.txt_Display_Average);
            this.gbx_Setup_Display.Controls.Add(this.label18);
            this.gbx_Setup_Display.Controls.Add(this.label16);
            this.gbx_Setup_Display.Controls.Add(this.txt_Display_Sampling);
            this.gbx_Setup_Display.Controls.Add(this.label14);
            this.gbx_Setup_Display.Location = new System.Drawing.Point(43, 343);
            this.gbx_Setup_Display.Name = "gbx_Setup_Display";
            this.gbx_Setup_Display.Size = new System.Drawing.Size(332, 100);
            this.gbx_Setup_Display.TabIndex = 21;
            this.gbx_Setup_Display.TabStop = false;
            this.gbx_Setup_Display.Text = "Setup Display ";
            // 
            // txt_Display_Sampling
            // 
            this.txt_Display_Sampling.Location = new System.Drawing.Point(126, 23);
            this.txt_Display_Sampling.Name = "txt_Display_Sampling";
            this.txt_Display_Sampling.Size = new System.Drawing.Size(75, 20);
            this.txt_Display_Sampling.TabIndex = 5;
            this.txt_Display_Sampling.Text = "10";
            this.txt_Display_Sampling.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Location = new System.Drawing.Point(13, 26);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(112, 14);
            this.label14.TabIndex = 4;
            this.label14.Text = "Display Sampling = ";
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.Location = new System.Drawing.Point(211, 73);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(99, 14);
            this.label15.TabIndex = 4;
            this.label15.Text = "Sample/Seconds";
            // 
            // label16
            // 
            this.label16.AutoSize = true;
            this.label16.Location = new System.Drawing.Point(211, 26);
            this.label16.Name = "label16";
            this.label16.Size = new System.Drawing.Size(99, 14);
            this.label16.TabIndex = 5;
            this.label16.Text = "Sample/Seconds";
            // 
            // txt_File_Name
            // 
            this.txt_File_Name.Enabled = false;
            this.txt_File_Name.Location = new System.Drawing.Point(98, 188);
            this.txt_File_Name.Name = "txt_File_Name";
            this.txt_File_Name.Size = new System.Drawing.Size(212, 20);
            this.txt_File_Name.TabIndex = 19;
            // 
            // label17
            // 
            this.label17.AutoSize = true;
            this.label17.Location = new System.Drawing.Point(20, 191);
            this.label17.Name = "label17";
            this.label17.Size = new System.Drawing.Size(72, 14);
            this.label17.TabIndex = 20;
            this.label17.Text = "File Name = ";
            // 
            // txt_Display_Average
            // 
            this.txt_Display_Average.Location = new System.Drawing.Point(126, 60);
            this.txt_Display_Average.Name = "txt_Display_Average";
            this.txt_Display_Average.Size = new System.Drawing.Size(75, 20);
            this.txt_Display_Average.TabIndex = 7;
            this.txt_Display_Average.Text = "10";
            this.txt_Display_Average.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // label18
            // 
            this.label18.AutoSize = true;
            this.label18.Location = new System.Drawing.Point(13, 63);
            this.label18.Name = "label18";
            this.label18.Size = new System.Drawing.Size(107, 14);
            this.label18.TabIndex = 6;
            this.label18.Text = "Display Average = ";
            // 
            // frmDataDisplay
            // 
            this.AcceptButton = this.cmdStartConvert;
            this.AutoScaleBaseSize = new System.Drawing.Size(6, 13);
            this.BackColor = System.Drawing.SystemColors.Window;
            this.ClientSize = new System.Drawing.Size(439, 509);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.lblInstruction);
            this.Controls.Add(this.lblShowData);
            this.Controls.Add(this.txtNumChan);
            this.Controls.Add(this.lblValueRead);
            this.Controls.Add(this.lblChanPrompt);
            this.Controls.Add(this.lblDemoFunction);
            this.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ForeColor = System.Drawing.SystemColors.WindowText;
            this.Location = new System.Drawing.Point(182, 100);
            this.Name = "frmDataDisplay";
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "DAQ Rapid";
            this.Load += new System.EventHandler(this.frmDataDisplay_Load);
            this.gbx_Setup_Amplifier.ResumeLayout(false);
            this.gbx_Setup_Amplifier.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage2.ResumeLayout(false);
            this.gbx_Setup_DAQ.ResumeLayout(false);
            this.gbx_Setup_DAQ.PerformLayout();
            this.gbx_Setup_Display.ResumeLayout(false);
            this.gbx_Setup_Display.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

		}

	#endregion

        #region Form initialization, variables, and entry point

        /// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main() 
		{
			Application.Run(new frmDataDisplay());
		}

        public frmDataDisplay()
        {

            // This call is required by the Windows Form Designer.
            InitializeComponent();

        }

        // Form overrides dispose to clean up the component list.
        protected override void Dispose(bool Disposing)
        {
            if (Disposing)
            {
                if (components != null)
                {
                    components.Dispose();
                }
            }
            base.Dispose(Disposing);
        }

        // Required by the Windows Form Designer
        private System.ComponentModel.IContainer components;
        public ToolTip ToolTip1;
        public Button cmdStartConvert;
        public Button cmdStopConvert;
        public TextBox txtNumChan;
        //public Timer tmrConvert;
        public Label lblShowVoltsCh0;
        public Label lblVoltsRead;
        public Label lblValueRead;
        public Label lblChanPrompt;
        public Label lblInstruction;

        private void btnExportData_Click(object sender, EventArgs e)
        {
            if(DataDaqList.Count == 0)
            {
                MessageBox.Show("No data to export, please try again", "Export the data",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                return;
            }
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.FileName = "Data_DAQ" + DateTime.Now.ToString("_yyyy_dd_M__HH_mm_ss");
            sfd.Filter = "CSV File | *.csv";
            using (sfd)
            {
                if (sfd.ShowDialog() == DialogResult.OK)
                {

                    using (StreamWriter sr = File.CreateText(sfd.FileName))
                    {

                        // write data UserCodeFormated
                        for (int i = 0; i < DataDaqList.Count; i++)
                        {
                            sr.WriteLine(DataDaqList[i]);

                        }

                        sr.Close();
                    }
                }
            }


        }

        private void groupBox2_Enter(object sender, EventArgs e)
        {

        }

        private void btnTest_Click(object sender, EventArgs e)
        {
            // Display the timer frequency and resolution.
            if (Stopwatch.IsHighResolution)
            {
                //AddMessage("Using the system's high-resolution performance counter.");
                MessageBox.Show("High Resolution", "Timer",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            }
            else
            {
                //AddMessage("Using the DateTime class.");
                MessageBox.Show("Low Resolution", "Timer",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            }

            long frequency = Stopwatch.Frequency;
            //AddMessage(string.Format("Timer frequency in ticks per second = {0}", frequency));
            MessageBox.Show(string.Format("Timer frequency in ticks per second = {0}", frequency), "Timer",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            long nanosecPerTick = (1000L * 1000L * 1000L) / frequency;
            //AddMessage(string.Format("Timer is accurate to within {0} nanoseconds", nanosecPerTick));
            MessageBox.Show(string.Format(string.Format("Timer is accurate to within {0} nanoseconds", nanosecPerTick)), "Timer",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            DataDaqList.Add(DateTime.Now.ToString("MM / dd / yyyy hh: mm:ss.fffffff tt"));

            sw.Start();
            while(sw.ElapsedTicks < 10){};

            sw.Stop();
            DataDaqList.Add(DateTime.Now.ToString("MM / dd / yyyy hh: mm:ss.fffffff tt"));
        }

        private void chkRecordData_CheckedChanged(object sender, EventArgs e)
        {
            if (chkRecordData.Checked == true)
            {
                Record_Data = true; 
            }
            else
            {
                Record_Data = false;
            }
        }

        private void txtVoltsToSet_TextChanged(object sender, EventArgs e)
        {

        }

        private void txtNumChan_TextChanged(object sender, EventArgs e)
        {

        }

        public Label lblShowData;

        #endregion

    }
}