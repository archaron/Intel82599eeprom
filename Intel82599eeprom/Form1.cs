using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Intel82599eeprom
{



    public partial class Form1 : Form
    {

        byte[] eeprom;
        public string[] PCIeOptionsStrings = {
            "PCIe Init Configuration 1",
            "PCIe Init Configuration 2",
            "PCIe Init Configuration 3",
            "PCIe Control 1",
            "PCIe Control 2",
            "PCIe LAN Power Consumption",
            "PCIe Control 3",
            "PCIe Sub-System ID",
            "PCIe Sub-System Vendor ID",
            "PCIe Dummy Device ID",
            "PCIe Device Revision ID",
            "IOV Control Word 1",
            "IOV Control Word 2",
            "Reserved",
            "Reserved",
            "Reserved",
            "MAC byte 1",
            "MAC byte 2",
            "MAC byte 3",
            "PCIe L1 Exit latencies",
            "Spare"
        };

        public string[] PCIeSpaceStrings = {
            "Control Word",
            "Device ID",
            "CDQM Memory Base 0/1 Low",
            "CDQM Memory Base 0/1 High",
            "Reserved"
        };

        public string[] LANCoreStrings = {
           "MAC Bytes 2/1",
           "MAC Bytes 4/3",
           "MAC Bytes 6/5",
           "LED 1/0 Configuration",
           "LED 3/2 Configuration",
           "SDP Control",
           "Filter Control"
        };

        public string[] MACStrings = {
            "Link Mode",
            "Swap Configuration",
            "Swizzle and Polarity",
            "Auto Negotiation Default Bits",
            "AUTOC2 Upper Half",
            "SGMIIC Lower Half",
            "KR-PCS"
        };

        public UInt16 macStart;
        public UInt16 IDsStart;
        public UInt16 space0IDStart;
        public UInt16 space1IDStart;

        public UInt16 LAN0MACStart;
        public UInt16 LAN1MACStart;

        public Form1()
        {
            InitializeComponent();
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            DialogResult result = openFileDialog1.ShowDialog();
            if (result.ToString() == "OK")
            {
                tbFileName.Text = openFileDialog1.FileName;
            }
        }

        private void btnLoad_Click(object sender, EventArgs e)
        {
            if (tbFileName.Text.Length != 0)
            {
                FileStream stream = new FileStream(tbFileName.Text, FileMode.Open);
                eeprom = new byte[16384];
                stream.Read(eeprom, 0, 16384);
                stream.Close();
                ParseEEPROM();
            }
        }

        private void ParseEEPROM()
        {
            if (eeprom != null)
            {
                UInt16 ControlWord = BitConverter.ToUInt16(eeprom, 0);
                tbControlWord.Text = ControlWord.ToString("X4");

                // Check bits 7 and 6, must be 01 in case of valid eeprom
                if ((ControlWord & 192) == 64)
                {
                    lbValid.Text = "VALID";
                    lbValid.ForeColor = Color.Green;
                }
                else
                {
                    lbValid.Text = "INVALID";
                    lbValid.ForeColor = Color.Red;
                }

                cbManagable.Checked = IsBitSet(ControlWord, 10);
                cbProtected.Checked = IsBitSet(ControlWord, 11);



                UInt16 hbsize = (UInt16)(ControlWord & 15);
                lbHidden.Text = hbsize > 0 ? ("Hidden block size: " + ((UInt16)Math.Pow(2, hbsize)).ToString() + " KB") : "No hidden block";

                UInt16 Reserved1 = (UInt16)((ControlWord & 0xF000) >> 12);
                tbReserved.Text = Reserved1.ToString("X1");

                UInt16 EEPROMSize = (UInt16)(Math.Pow(2, ((ControlWord & 0x0F00) >> 8)) / 8);
                tbSize.Text = EEPROMSize.ToString();


                UInt16 ControlWord2 = BitConverter.ToUInt16(eeprom, 2);
                tbControlWord2.Text = ControlWord2.ToString("X4");

                UInt16 Reserved2 = (UInt16)((ControlWord2 & 0xFF80) >> 7);
                tbReserved2.Text = Reserved2.ToString("X3");
                cbGateDis.Checked = IsBitSet(ControlWord2, 6);

                UInt16 Reserved3 = (UInt16)((ControlWord2 & 0x0038) >> 3);
                tbReserved3.Text = Reserved3.ToString("X1");

                cbXAUI.Checked = IsBitSet(ControlWord2, 2);
                cbClock.Checked = IsBitSet(ControlWord2, 1);
                cbPCIe.Checked = IsBitSet(ControlWord2, 0);

                UInt16 ControlWord3 = BitConverter.ToUInt16(eeprom, 0x38 * 2);
                tbControlWord3.Text = ControlWord3.ToString("X4");

                UInt16 Reserved4 = (UInt16)((ControlWord3 & 0xFFFC) >> 2);
                tbReserved4.Text = Reserved4.ToString("X4");

                cbAPM1.Checked = IsBitSet(ControlWord3, 1);
                cbAPM0.Checked = IsBitSet(ControlWord3, 0);

                UInt16 PCIAnalogModule = BitConverter.ToUInt16(eeprom, 0x3 * 2);
                cbPCIanalog.Text = PCIAnalogModule.ToString("X4");

                if (PCIAnalogModule < 8192)
                {
                    List<PCIaSection> PCIaSections = new List<PCIaSection>();
                    int PCIaSectionsNum = BitConverter.ToUInt16(eeprom, PCIAnalogModule * 2) / 2;

                    for (int i = 0; i < PCIaSectionsNum; i++)
                    {
                        PCIaSection section = new PCIaSection();
                        section.address = BitConverter.ToUInt16(eeprom, PCIAnalogModule * 2 + i * 4 + 2).ToString("X4");
                        section.data = BitConverter.ToUInt16(eeprom, PCIAnalogModule * 2 + i * 4 + 3).ToString("X4");
                        PCIaSections.Add(section);
                    }

                    dgPCIaSections.DataSource = PCIaSections;
                }

                UInt16 Core0AnalogModule = BitConverter.ToUInt16(eeprom, 0x4 * 2);
                tbCore0Analog.Text = Core0AnalogModule.ToString("X4");
                if (Core0AnalogModule < 8192)
                {
                    List<PCIaSection> Core0Sections = new List<PCIaSection>();
                    int Core0SectionsNum = BitConverter.ToUInt16(eeprom, Core0AnalogModule * 2) / 2;

                    for (int i = 0; i < Core0SectionsNum; i++)
                    {
                        PCIaSection section = new PCIaSection();
                        section.address = BitConverter.ToUInt16(eeprom, Core0AnalogModule * 2 + i * 4 + 2).ToString("X4");
                        section.data = BitConverter.ToUInt16(eeprom, Core0AnalogModule * 2 + i * 4 + 3).ToString("X4");
                        Core0Sections.Add(section);
                    }

                    dgCore0Analog.DataSource = Core0Sections;
                }

                UInt16 Core1AnalogModule = BitConverter.ToUInt16(eeprom, 0x5 * 2);
                tbCore1Analog.Text = Core1AnalogModule.ToString("X4");
                if (Core1AnalogModule < 8192)
                {
                    List<PCIaSection> Core1Sections = new List<PCIaSection>();
                    int Core1SectionsNum = BitConverter.ToUInt16(eeprom, Core1AnalogModule * 2) / 2;

                    for (int i = 0; i < Core1SectionsNum; i++)
                    {
                        PCIaSection section = new PCIaSection();
                        section.address = BitConverter.ToUInt16(eeprom, Core1AnalogModule * 2 + i * 4 + 2).ToString("X4");
                        section.data = BitConverter.ToUInt16(eeprom, Core1AnalogModule * 2 + i * 4 + 3).ToString("X4");
                        Core1Sections.Add(section);
                    }

                    dgCore1Analog.DataSource = Core1Sections;
                }

                UInt16 PCIeConfig = BitConverter.ToUInt16(eeprom, 0x6 * 2);
                tbPCIeConfig.Text = PCIeConfig.ToString("X4");

                if (PCIeConfig < 8192)
                {
                    List<PCIeOption> PCIeOptions = new List<PCIeOption>();
                    int PCIeOptionsNum = BitConverter.ToUInt16(eeprom, PCIeConfig * 2);

                    for (int i = 0; i < PCIeOptionsNum; i++)
                    {
                        PCIeOption option = new PCIeOption();
                        option.name = i < PCIeOptionsStrings.Length ? PCIeOptionsStrings[i] : "Unknown";
                        option.value = BitConverter.ToUInt16(eeprom, PCIeConfig * 2 + i * 2 + 2).ToString("X4");
                        PCIeOptions.Add(option);
                    }

                    dgPCIeConfig.DataSource = PCIeOptions;
                    macStart = (UInt16)((int)PCIeConfig * 2 + 34);
                    IDsStart = (UInt16)((int)PCIeConfig * 2 + 16);
                    tbPMAC.Text = ReverseBytes(BitConverter.ToUInt16(eeprom, macStart)).ToString("X4")
                                + ReverseBytes(BitConverter.ToUInt16(eeprom, macStart + 2)).ToString("X4")
                                + ReverseBytes(BitConverter.ToUInt16(eeprom, macStart + 4)).ToString("X4");

                    tbPMAC.Enabled = true;

                    tbSubsystemID.Text = BitConverter.ToUInt16(eeprom, IDsStart).ToString("X4");
                    tbSubsystemID.Enabled = true;

                    tbVenorID.Text = BitConverter.ToUInt16(eeprom, IDsStart + 2).ToString("X4");
                    tbVenorID.Enabled = true;

                    tbDummyDeviceID.Text = BitConverter.ToUInt16(eeprom, IDsStart + 4).ToString("X4");
                    tbDummyDeviceID.Enabled = true;

                    tbRevisionID.Text = BitConverter.ToUInt16(eeprom, IDsStart + 6).ToString("X4");
                    tbRevisionID.Enabled = true;


                }


                UInt16 PCIeSpace0 = BitConverter.ToUInt16(eeprom, 0x7 * 2);
                tbPCIeSpace0.Text = PCIeSpace0.ToString("X4");

                UInt16 PCIeSpace1 = BitConverter.ToUInt16(eeprom, 0x8 * 2);
                tbPCIeSpace1.Text = PCIeSpace1.ToString("X4");


                if (PCIeSpace0 < 8192)
                {
                    List<PCIeOption> PCIeSpace0Options = new List<PCIeOption>();
                    int PCIeSpace0OptionsNum = BitConverter.ToUInt16(eeprom, PCIeSpace0 * 2);

                    for (int i = 0; i < PCIeSpace0OptionsNum; i++)
                    {
                        PCIeOption option = new PCIeOption();
                        option.name = i < PCIeSpaceStrings.Length ? PCIeSpaceStrings[i] : "Unknown";
                        option.value = BitConverter.ToUInt16(eeprom, PCIeSpace0 * 2 + i * 2 + 2).ToString("X4");
                        PCIeSpace0Options.Add(option);
                    }
                    space0IDStart = (UInt16)((int)PCIeSpace0 * 2 + 4);

                    tbSpace0ID.Text = BitConverter.ToUInt16(eeprom, space0IDStart).ToString("X4");
                    tbSpace0ID.Enabled = true;
                    dgSpace0.DataSource = PCIeSpace0Options;
                }

                if (PCIeSpace1 < 8192)
                {
                    List<PCIeOption> PCIeSpace1Options = new List<PCIeOption>();
                    int PCIeSpace1OptionsNum = BitConverter.ToUInt16(eeprom, PCIeSpace1 * 2);

                    for (int i = 0; i < PCIeSpace1OptionsNum; i++)
                    {
                        PCIeOption option = new PCIeOption();
                        option.name = i < PCIeSpaceStrings.Length ? PCIeSpaceStrings[i] : "Unknown";
                        option.value = BitConverter.ToUInt16(eeprom, PCIeSpace1 * 2 + i * 2 + 2).ToString("X4");
                        PCIeSpace1Options.Add(option);
                    }
                    space1IDStart = (UInt16)((int)PCIeSpace1 * 2 + 4);
                    tbSpace1ID.Text = BitConverter.ToUInt16(eeprom, space1IDStart).ToString("X4");
                    tbSpace1ID.Enabled = true;
                    dgSpace1.DataSource = PCIeSpace1Options;
                }


                UInt16 LAN0Core = BitConverter.ToUInt16(eeprom, 0x9 * 2);
                tbLAN1.Text = LAN0Core.ToString("X4");

                UInt16 LAN1Core = BitConverter.ToUInt16(eeprom, 0xa * 2);
                tbLAN2.Text = LAN1Core.ToString("X4");


                if (LAN0Core < 8192)
                {
                    List<PCIeOption> LAN0CoreOptions = new List<PCIeOption>();
                    int LAN0CoreOptionsNum = BitConverter.ToUInt16(eeprom, LAN0Core * 2);

                    for (int i = 0; i < LAN0CoreOptionsNum; i++)
                    {
                        PCIeOption option = new PCIeOption();
                        option.name = i < LANCoreStrings.Length ? LANCoreStrings[i] : "Unknown";
                        option.value = BitConverter.ToUInt16(eeprom, LAN0Core * 2 + i * 2 + 2).ToString("X4");
                        LAN0CoreOptions.Add(option);
                    }
                    LAN0MACStart = (UInt16)((int)LAN0Core * 2 + 2);
                    tbLAN0MAC.Text = ReverseBytes(BitConverter.ToUInt16(eeprom, LAN0MACStart)).ToString("X4")
                        + ReverseBytes(BitConverter.ToUInt16(eeprom, LAN0MACStart + 2)).ToString("X4")
                        + ReverseBytes(BitConverter.ToUInt16(eeprom, LAN0MACStart + 4)).ToString("X4");
                    dgLAN0.DataSource = LAN0CoreOptions;
                }

                if (LAN1Core < 8192)
                {
                    List<PCIeOption> LAN1CoreOptions = new List<PCIeOption>();
                    int LAN1CoreOptionsNum = BitConverter.ToUInt16(eeprom, LAN1Core * 2);

                    for (int i = 0; i < LAN1CoreOptionsNum; i++)
                    {
                        PCIeOption option = new PCIeOption();
                        option.name = i < LANCoreStrings.Length ? LANCoreStrings[i] : "Unknown";
                        option.value = BitConverter.ToUInt16(eeprom, LAN1Core * 2 + i * 2 + 2).ToString("X4");
                        LAN1CoreOptions.Add(option);
                    }
                    LAN1MACStart = (UInt16)((int)LAN1Core * 2 + 2);
                    tbLAN1MAC.Text = ReverseBytes(BitConverter.ToUInt16(eeprom, LAN1MACStart)).ToString("X4")
                                            + ReverseBytes(BitConverter.ToUInt16(eeprom, LAN1MACStart + 2)).ToString("X4")
                                            + ReverseBytes(BitConverter.ToUInt16(eeprom, LAN1MACStart + 4)).ToString("X4");
                    dgLAN1.DataSource = LAN1CoreOptions;
                }


                UInt16 MAC0 = BitConverter.ToUInt16(eeprom, 0xb * 2);
                tbMAC0.Text = MAC0.ToString("X4");

                UInt16 MAC1 = BitConverter.ToUInt16(eeprom, 0xc * 2);
                tbMAC1.Text = MAC1.ToString("X4");


                if (MAC0 < 8192)
                {
                    List<PCIeOption> MAC0Options = new List<PCIeOption>();
                    int MAC0OptionsNum = BitConverter.ToUInt16(eeprom, MAC0 * 2);

                    for (int i = 0; i < MAC0OptionsNum; i++)
                    {
                        PCIeOption option = new PCIeOption();
                        option.name = i < MACStrings.Length ? MACStrings[i] : "Unknown";
                        option.value = BitConverter.ToUInt16(eeprom, MAC0 * 2 + i * 2 + 2).ToString("X4");
                        MAC0Options.Add(option);
                    }

                    dgMAC0.DataSource = MAC0Options;
                }

                if (MAC1 < 8192)
                {
                    List<PCIeOption> MAC1Options = new List<PCIeOption>();
                    int MAC1OptionsNum = BitConverter.ToUInt16(eeprom, MAC1 * 2);

                    for (int i = 0; i < MAC1OptionsNum; i++)
                    {
                        PCIeOption option = new PCIeOption();
                        option.name = i < MACStrings.Length ? MACStrings[i] : "Unknown";
                        option.value = BitConverter.ToUInt16(eeprom, MAC1 * 2 + i * 2 + 2).ToString("X4");
                        MAC1Options.Add(option);
                    }

                    dgMAC1.DataSource = MAC1Options;
                }

                UInt16 CSR0 = BitConverter.ToUInt16(eeprom, 0xd * 2);
                tbCSR0.Text = CSR0.ToString("X4");

                UInt16 CSR1 = BitConverter.ToUInt16(eeprom, 0xe * 2);
                tbCSR1.Text = CSR1.ToString("X4");

                if (CSR0 < 8192)
                {
                    List<PCIaSection> CSR0Sections = new List<PCIaSection>();
                    int CSR0SectionsNum = BitConverter.ToUInt16(eeprom, CSR0 * 2) / 3;

                    for (int i = 0; i < CSR0SectionsNum; i++)
                    {
                        PCIaSection section = new PCIaSection();
                        section.address = BitConverter.ToUInt16(eeprom, CSR0 * 2 + i * 6 + 2).ToString("X4");
                        section.data = BitConverter.ToUInt32(eeprom, CSR0 * 2 + i * 6 + 3).ToString("X8");
                        CSR0Sections.Add(section);
                    }

                    dgCSR0.DataSource = CSR0Sections;
                }

                if (CSR1 < 8192)
                {
                    List<PCIaSection> CSR1Sections = new List<PCIaSection>();
                    int CSR1SectionsNum = BitConverter.ToUInt16(eeprom, CSR1 * 2) / 3;

                    for (int i = 0; i < CSR1SectionsNum; i++)
                    {
                        PCIaSection section = new PCIaSection();
                        section.address = BitConverter.ToUInt16(eeprom, CSR1 * 2 + i * 6 + 2).ToString("X4");
                        section.data = BitConverter.ToUInt32(eeprom, CSR1 * 2 + i * 6 + 3).ToString("X8");
                        CSR1Sections.Add(section);
                    }

                    dgCSR1.DataSource = CSR1Sections;
                }

                UInt16 FWBase = 0xf * 2;

                UInt16 FWTestConfigPointer = BitConverter.ToUInt16(eeprom, FWBase);
                UInt16 FWReserved = BitConverter.ToUInt16(eeprom, FWBase + 0x1 * 2);
                UInt16 FWLESM = BitConverter.ToUInt16(eeprom, FWBase + 0x2 * 2);
                UInt16 FWCommonParams = BitConverter.ToUInt16(eeprom, FWBase + 0x3 * 2);
                UInt16 FWPassThroughPatch = BitConverter.ToUInt16(eeprom, FWBase + 0x4 * 2);
                UInt16 FWPassThroughLAN0 = BitConverter.ToUInt16(eeprom, FWBase + 0x5 * 2);
                UInt16 FWSideBand = BitConverter.ToUInt16(eeprom, FWBase + 0x6 * 2);
                UInt16 FWTCOFilter = BitConverter.ToUInt16(eeprom, FWBase + 0x7 * 2);
                UInt16 FWPassThroughLAN1 = BitConverter.ToUInt16(eeprom, FWBase + 0x8 * 2);
                UInt16 FWNCSIDownload = BitConverter.ToUInt16(eeprom, FWBase + 0x9 * 2);
                UInt16 FWNCSIConfig = BitConverter.ToUInt16(eeprom, FWBase + 0xa * 2);

                tbTestConfig.Text = FWTestConfigPointer.ToString("X4");
                tbFWReserved.Text = FWReserved.ToString("X4");
                tbFWLESM.Text = FWLESM.ToString("X4");
                tbFWCommonParams.Text = FWCommonParams.ToString("X4");
                tbFWPassThroughPatch.Text = FWPassThroughPatch.ToString("X4");
                tbFWPassThroughLAN0.Text = FWPassThroughLAN0.ToString("X4");
                tbFWSideBand.Text = FWSideBand.ToString("X4");
                tbFWTCOFilter.Text = FWTCOFilter.ToString("X4");
                tbFWPassThroughLAN1.Text = FWPassThroughLAN1.ToString("X4");
                tbFWNCSIDownload.Text = FWNCSIDownload.ToString("X4");
                tbFWNCSIConfig.Text = FWNCSIConfig.ToString("X4");
                btnPatch.Enabled = true;

                UInt16 CRC = BitConverter.ToUInt16(eeprom, 0x3F * 2);
                tbCRC.Text = CRC.ToString("X4");

                UInt16 newCRC = CalculateCRC();
                tbNewCRC.Text = newCRC.ToString("X4");

                UInt16 SFPModule = BitConverter.ToUInt16(eeprom, 0x2B * 2);
                tbSFP.Text = SFPModule.ToString("X4");


                if (CRC != newCRC)
                {
                    lbCRC.Text = "NOT VALID";
                    lbCRC.ForeColor = Color.Red;
                } else
                {
                    lbCRC.Text = "VALID";
                    lbCRC.ForeColor = Color.Green;
                }
            }
        }

        public byte[] Hex2Bytes(string input)
        {
            byte[] data = new byte[(int)Math.Ceiling((double)input.Length / 2)];

            var builder = new StringBuilder();
            for (int i = 0; i < input.Length; i += 2)
            { //throws an exception if not properly formatted
                string hexdec = input.Substring(i, 2);
                data[i / 2] = (byte)Int32.Parse(hexdec, NumberStyles.HexNumber);
            }
            return data;
        }

        public string BytesToHex(byte[] bytes)
        {
            string hex = "";
            foreach (char c in bytes)
            {
                int tmp = c;
                hex += String.Format("{0:x2}", (uint)System.Convert.ToUInt32(tmp.ToString()));
            }
            return hex;
        }

        public static UInt16 ReverseBytes(UInt16 value)
        {
            return (UInt16)((value & 0xFFU) << 8 | (value & 0xFF00U) >> 8);
        }

        public static bool IsBitSet(UInt16 word, int bit)
        {
            return (word & (UInt16)Math.Pow(2, bit)) == ((UInt16)Math.Pow(2, bit));
        }



        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void cbProtected_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void tbPMAC_KeyPress(object sender, KeyPressEventArgs e)
        {



        }

        private void tbPMAC_KeyPress(object sender, KeyEventArgs e)
        {

        }

        private void tbPMAC_KeyPress(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                UInt64 MAC = UInt64.Parse("0000" + tbPMAC.Text, NumberStyles.HexNumber);
                tbLAN0MAC.Text = (MAC).ToString("X12");
                tbLAN1MAC.Text = (MAC + 1).ToString("X12");
            }
            catch (Exception ex)
            {
                tbLAN0MAC.Text = (0).ToString("X12");
                tbLAN1MAC.Text = (1).ToString("X12");

            }
        }

        private void btnPatch_Click(object sender, EventArgs e)
        {
            //            UInt16 SubsytemID = ReverseBytes(UInt16.Parse(tbSubsystemID.Text, NumberStyles.HexNumber));

            UInt16[] ids = {
                (UInt16.Parse(tbSubsystemID.Text, NumberStyles.HexNumber)),
                (UInt16.Parse(tbVenorID.Text, NumberStyles.HexNumber)),
                (UInt16.Parse(tbDummyDeviceID.Text, NumberStyles.HexNumber)),
                (UInt16.Parse(tbRevisionID.Text, NumberStyles.HexNumber))
            };
            Buffer.BlockCopy(ids, 0, eeprom, IDsStart, 8);

            UInt16 Space0ID = (UInt16.Parse(tbSpace0ID.Text, NumberStyles.HexNumber));
            UInt16 Space1ID = (UInt16.Parse(tbSpace1ID.Text, NumberStyles.HexNumber));


            BitConverter.GetBytes(Space0ID).CopyTo(eeprom, space0IDStart);
            BitConverter.GetBytes(Space1ID).CopyTo(eeprom, space1IDStart);

            Byte[] MAC0 = Hex2Bytes(tbLAN0MAC.Text.Substring(0, 12));
            Byte[] MAC1 = Hex2Bytes(tbLAN1MAC.Text.Substring(0, 12));

            Byte[] MAC = Hex2Bytes(tbPMAC.Text.Substring(0, 12));

            MAC0.CopyTo(eeprom, LAN0MACStart);
            MAC1.CopyTo(eeprom, LAN1MACStart);
            MAC.CopyTo(eeprom, macStart);

            UInt16 CRC = BitConverter.ToUInt16(eeprom, 0x3F * 2);
            UInt16 newCRC = CalculateCRC();

            // Update CRC if needed
            if (CRC != newCRC)
            {
                BitConverter.GetBytes(newCRC).CopyTo(eeprom, 0x3F * 2);
            }

            btnSave.Enabled = true;
            ParseEEPROM();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            DialogResult result = saveFileDialog1.ShowDialog();
            if (result.ToString() == "OK")
            {
                Stream File = saveFileDialog1.OpenFile();
                File.Write(eeprom, 0, 16384);
                File.Close();
            }
        }

        protected UInt16 CalculateCRC()
        {
            UInt16 i;
            UInt16 j;
            UInt16 checksum = 0;
            UInt16 length = 0;
            UInt16 pointer = 0;
            UInt16 word = 0;
            
            // Include 0x0-0x3F in the checksum 
            for (i = 0; i < 0x3F; i++)
            {
                checksum += BitConverter.ToUInt16(eeprom, i * 2);
                checksum += word;
            }

            /* Include all data from pointers except for the fw pointer */

            for (i = 3; i < 0xf; i++)
            {
                pointer = BitConverter.ToUInt16(eeprom, i * 2);

                /* Make sure the pointer seems valid */
                if (pointer != 0xFFFF && pointer != 0)
                {
                    length = BitConverter.ToUInt16(eeprom, pointer * 2);
                    if (length != 0xFFFF && length != 0)
                    {
                        for (j = (UInt16)((int)pointer + 1); j <= pointer + length; j++)
                        {
                            word = BitConverter.ToUInt16(eeprom, j * 2);
                            checksum += word;
                        }
                    }
                }
            }

            checksum = (UInt16)(((UInt16)0xBABA) - checksum);
            return checksum;
        }

    }
    public class PCIaSection
    {
        public string address { get; set; }
        public string data { get; set; }
    }

    public class PCIeOption
    {
        public string name { get; set; }
        public string value { get; set; }
    }



}
