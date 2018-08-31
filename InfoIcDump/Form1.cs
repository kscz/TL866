/*
 * radioman 2013 -2018
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.IO;
using System.Xml.Serialization;
using System.Xml;


namespace InfoIcDump
{

    public partial class Form1
    {
        //System API
        [DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr LoadLibrary(string lpFileName);
        [DllImport("kernel32", CharSet = CharSet.Ansi, SetLastError = true)]
        private static extern bool FreeLibrary(IntPtr hModule);
        [DllImport("user32", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr LoadBitmap(IntPtr hInstance, int lpBitmapName);
        [DllImport("gdi32", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool DeleteObject(IntPtr hObject);
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        //InfoIc functions
        [DllImport("InfoIC.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        private static extern void GetMfcStru(uint Manuf, ref MfcStruct mfstr);
        [DllImport("InfoIC.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        private static extern void GetIcStru(uint Manuf, uint device, ref DevStruct IcName);
        [DllImport("InfoIC.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        private static extern uint GetIcMFC(string search, uint[] ManArray, uint IcType);
        [DllImport("InfoIC.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        private static extern uint GetIcList(string search, uint[] ICArray, uint Manuf, uint IcType);
        [DllImport("InfoIC.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        private static extern uint GetDllInfo(ref uint p1, ref uint p2);


        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        public struct MfcStruct
        {
            public uint manufacturer;
            public uint logo;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 20)]
            public string manufacturer_name;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            public string manufacturer_description;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        public struct DevStruct
        {
            public uint protocol;
            public uint unknown;
            public uint category;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 40)]
            public string name;
            public uint type;
            public uint code_memory_size;
            public uint data_memory_size;
            public uint data_memory2_size;
            public ushort opts7;
            public ushort read_buffer_size;
            public ushort write_buffer_size;
            public ushort opts1;
            public uint opts2;
            public uint opts3;
            public uint chip_id;
            public uint opts5;
            public uint chip_id_size;
            public uint opts6;
            public uint package_details;
            public uint opts4;
        }


        public class device
        {
            [XmlAttribute("name")]
            public string icname;
            [XmlAttribute("protocol")]
            public string protocol;
            [XmlAttribute("variant")]
            public string type;
            [XmlAttribute("read_buffer_size")]
            public string read_buffer_size;
            [XmlAttribute("write_buffer_size")]
            public string write_buffer_size;
            [XmlAttribute("code_memory_size")]
            public string code_memory_size;
            [XmlAttribute("data_memory_size")]
            public string data_memory_size;
            [XmlAttribute("data_memory2_size")]
            public string data_memory2_size;
            [XmlAttribute("chip_id")]
            public string chip_id;
            [XmlAttribute("chip_id_size")]
            public string chip_id_size;
            [XmlAttribute("opts1")]
            public string opts1;
            [XmlAttribute("opts2")]
            public string opts2;
            [XmlAttribute("opts3")]
            public string opts3;
            [XmlAttribute("opts4")]
            public string opts4;
            [XmlAttribute("opts5")]
            public string opts5;
            [XmlAttribute("opts6")]
            public string opts6;
            [XmlAttribute("opts7")]
            public string opts7;
            [XmlAttribute("package_details")]
            public string package_details;
        }

        //constructor
        public Form1()
        {
            IntPtr Hmodule = LoadLibrary("InfoIc.dll");
            if (Hmodule == IntPtr.Zero)
            {
                if (MessageBox.Show(this, "InfoIc.dll was not found!\n Do you want to load it from other place?",
                    "Load error", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) == System.Windows.Forms.DialogResult.Yes)
                        load_infoic();
            }

            InitializeComponent();
            populate_mfc_list();
        }


        //Load the infoic.dll
        private void load_infoic()
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Title = "InfoIc.dll";
            dlg.Filter = "dll files (*.dll)|*.dll|All files (*.*)|*.*";
            dlg.CheckFileExists = true;
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                //Workaround to unload the infoic.dll
               while(FreeLibrary(GetModuleHandle("InfoIc.dll")));
               FreeLibrary(GetModuleHandle("InfoIc.dll"));

                //Load the new library
               IntPtr Hmodule = LoadLibrary(dlg.FileName);
                if (Hmodule == IntPtr.Zero)
                    MessageBox.Show(this, "Error loading the " + dlg.FileName,
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        //device type was changed
        private void RadioButton_CheckedChanged(System.Object sender, System.EventArgs e)
        {
            populate_mfc_list();
        }

        //manufacturers list selection was changed
        private void MfcList_SelectedIndexChanged(System.Object sender, System.EventArgs e)
        {
            MfcStruct mfcstruct = new MfcStruct();
            uint[] tag = (uint[])MfcList.Tag;
            GetMfcStru(tag[MfcList.SelectedIndex], ref mfcstruct);
            LogoImage.Image = GetBitmapFromResources(mfcstruct.logo);
            Label1.Text = mfcstruct.manufacturer_description;
            uint[] devices = new uint[4096];
            DevStruct devstruct = new DevStruct();
            DeviceList.Items.Clear();
            for (int i = 0; i < GetIcList(SearchBox.Text.ToUpper(), devices, (uint)tag[MfcList.SelectedIndex], GetIcType()); i++)
            {
                GetIcStru((uint)tag[MfcList.SelectedIndex], devices[i], ref devstruct);
                DeviceList.Items.Add(devstruct.name);
            }
            DeviceList.Tag = devices;
            if (DeviceList.Items.Count > 0)
                DeviceList.SelectedIndex = 0;
            label_mfc.Text = "Manufacturers:" + MfcList.Items.Count.ToString();
        }

        //device list selection was changed
        private void DeviceList_SelectedIndexChanged(System.Object sender, System.EventArgs e)
        {
            DevStruct device = new DevStruct();
            uint[] tag1 = (uint[])MfcList.Tag;
            uint[] tag2 = (uint[])DeviceList.Tag;
            GetIcStru(tag1[MfcList.SelectedIndex], tag2[DeviceList.SelectedIndex], ref device);
            txt_info.Text = get_ic_string_ini(tag1[MfcList.SelectedIndex], tag2[DeviceList.SelectedIndex], ref device).ToString();
            label_devs.Text = "Devices:" + DeviceList.Items.Count.ToString();
        }

        //export type selection was changed
        private void checkBox_CheckedChanged(object sender, EventArgs e)
        {
            Button1.Enabled = (checkBox1.Checked || checkBox2.Checked || checkBox3.Checked || checkBox4.Checked);
        }


        private void button2_Click(object sender, EventArgs e)
        {
            load_infoic();
            populate_mfc_list();
        }


        //start the infoic.dll dump
        private void Button1_Click(System.Object sender, System.EventArgs e)
        {
            dump_database();
        }

        //get the category
        private uint GetIcType()
        {
            if (RadioAll.Checked)
                return 0;
            else if (RadioRom.Checked)
                return 1;
            else if (RadioMcu.Checked)
                return 2;
            else if (RadioPld.Checked)
                return 3;
            else if (RadioRam.Checked)
                return 4;
            return 5;
        }


        //Populate the manufacturers list
        private void populate_mfc_list()
        {
            try
            {
                uint[] manufacturers = new uint[4096];
                MfcStruct b = new MfcStruct();
                MfcList.Items.Clear();
                DeviceList.Items.Clear();
                for (int i = 0; i < GetIcMFC(SearchBox.Text.ToUpper(), manufacturers, GetIcType()); i++)
                {
                    GetMfcStru(manufacturers[i], ref b);
                    MfcList.Items.Add(b.manufacturer_name);
                }
                MfcList.Tag = manufacturers;
                if (MfcList.Items.Count > 0)
                    MfcList.SelectedIndex = 0;
                uint p1 = 0;
                uint p2 = 0;
                label_total.Text = "Total Devices:" + GetDllInfo(ref p1, ref p2).ToString();
            }
            catch
            {
                foreach (Control control in this.Controls)
                {
                        control.Enabled = false;
                }
            }
        }


        /*/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////*/
        uint change_endianess(uint value, uint size)
        {
            if (value == 0 || size == 0) return 0;// This is a database bug. Size is zero and id garabge bytes
            return (uint)((((int)value & 0xff) << 24) +
                (((uint)value & 0xff00) << 8) +
                (((uint)value & 0xff0000) >> 8) +
                (((uint)value & 0xff000000) >> 24))
                >> (int)(8 * (4 - size));
        }


        //Get device info in ini format
        private string get_ic_string_ini(uint manufacturer, uint device_index, ref DevStruct devstruct)
        {
            GetIcStru(manufacturer, device_index, ref devstruct);
            //Change endianess for the chip_id
            devstruct.chip_id = change_endianess(devstruct.chip_id, devstruct.chip_id_size);
            return string.Format(
@"[{0}]
protocol = 0x{1:x2}
variant = 0x{2:x2}
read_buffer_size =  0x{3:x2}
write_buffer_size = 0x{4:x2}
code_memory_size = 0x{5:x2}
data_memory_size = 0x{6:x2}
data_memory2_size = 0x{7:x2}
chip_id = 0x{8:x2}
chip_id_size = 0x{9:x2}
opts1 = 0x{10:x2}
opts2 = 0x{11:x2}
opts3 = 0x{12:x2}
opts4 = 0x{13:x2}
opts5 = 0x{14:x2}
opts6 = 0x{15:x2}
opts7 = 0x{16:x2}
package_details = 0x{17:x2}",
            devstruct.name.Trim(), devstruct.protocol, devstruct.type, devstruct.read_buffer_size,
            devstruct.write_buffer_size, devstruct.code_memory_size,
            devstruct.data_memory_size, devstruct.data_memory2_size,
            devstruct.chip_id, devstruct.chip_id_size, devstruct.opts1,
            devstruct.opts2, devstruct.opts3, devstruct.opts4, devstruct.opts5,
            devstruct.opts6, devstruct.opts7, devstruct.package_details);
        }

        //Get device info in c header format
        private string get_ic_string_c(uint manufacturer, uint device_index, ref DevStruct devstruct)
        {
            GetIcStru(manufacturer, device_index, ref devstruct);
            devstruct.chip_id = change_endianess(devstruct.chip_id, devstruct.chip_id_size);
            return string.Format(
@"{{
    .name = ""{0}"",
    .protocol = 0x{1:x2},
    .variant = 0x{2:x2},
    .read_buffer_size =  0x{3:x2},
    .write_buffer_size = 0x{4:x2},
    .code_memory_size = 0x{5:x2},
    .data_memory_size = 0x{6:x2},
    .data_memory2_size = 0x{7:x2},
    .chip_id = 0x{8:x2},
    .chip_id_size = 0x{9:x2},
    .opts1 = 0x{10:x2},
    .opts2 = 0x{11:x2},
    .opts3 = 0x{12:x2},
    .opts4 = 0x{13:x2},
    .opts5 = 0x{14:x2},
    .opts6 = 0x{15:x2},
    .opts7 = 0x{16:x2},
    .package_details = 0x{17:x2}
}},",
            devstruct.name.Trim(), devstruct.protocol, devstruct.type, devstruct.read_buffer_size,
            devstruct.write_buffer_size, devstruct.code_memory_size,
            devstruct.data_memory_size, devstruct.data_memory2_size,
            devstruct.chip_id, devstruct.chip_id_size, devstruct.opts1,
            devstruct.opts2, devstruct.opts3, devstruct.opts4, devstruct.opts5,
            devstruct.opts6, devstruct.opts7, devstruct.package_details);
        }


        //Get device info in xml format
        private device get_ic_xml(uint manufacturer, uint device_index, ref DevStruct devstruct)
        {
            device xml_chip = new device();
            GetIcStru(manufacturer, device_index, ref devstruct);
            xml_chip.icname = devstruct.name.Trim();
            xml_chip.protocol = "0x" + devstruct.protocol.ToString("x2");
            xml_chip.type = "0x" + devstruct.type.ToString("x2");
            xml_chip.type = "0x" + devstruct.type.ToString("x2");
            xml_chip.read_buffer_size = "0x" + devstruct.read_buffer_size.ToString("x2");
            xml_chip.write_buffer_size = "0x" + devstruct.write_buffer_size.ToString("x2");
            xml_chip.code_memory_size = "0x" + devstruct.code_memory_size.ToString("x2");
            xml_chip.data_memory_size = "0x" + devstruct.data_memory_size.ToString("x2");
            xml_chip.data_memory2_size = "0x" + devstruct.data_memory2_size.ToString("x2");
            xml_chip.chip_id = "0x" + devstruct.chip_id.ToString("x2");
            xml_chip.chip_id_size = "0x" + devstruct.chip_id_size.ToString("x2");
            xml_chip.opts1 = "0x" + devstruct.opts1.ToString("x2");
            xml_chip.opts2 = "0x" + devstruct.opts2.ToString("x2");
            xml_chip.opts3 = "0x" + devstruct.opts3.ToString("x2");
            xml_chip.opts4 = "0x" + devstruct.opts4.ToString("x2");
            xml_chip.opts5 = "0x" + devstruct.opts5.ToString("x2");
            xml_chip.opts6 = "0x" + devstruct.opts6.ToString("x2");
            xml_chip.opts7 = "0x" + devstruct.opts7.ToString("x2");
            xml_chip.package_details = "0x" + devstruct.package_details.ToString("x2");
            return xml_chip;
        }




        //Perform the infoic.dll dump
        private void dump_database()
        {
            uint[] manufacturers = new uint[4096];
            uint[] devices = new uint[4096];
            DevStruct devstruct = new DevStruct();
            List<device> device_list_xml = new List<device>();
            List<string> device_list_ini = new List<string>();
            List<string> device_list_c = new List<string>();
            SortedDictionary<uint, string> total = new SortedDictionary<uint, string>();

            progressBar.Maximum = (int)GetIcMFC("", manufacturers, 0);
            uint total_count = 0;

            //Iterate over the entire manufacturers
            for (uint i = 0; i < GetIcMFC("", manufacturers, 0); i++)
            {
                //Iterate over the entire devices in the curent manufacturer
                for (uint k = 0; k < GetIcList("", devices, manufacturers[i], 0); k++)
                {
                    //Log the device
                    if (total.ContainsKey(devstruct.protocol))
                        total[devstruct.protocol] += devstruct.name + Environment.NewLine;
                    else
                        total.Add(devstruct.protocol, devstruct.name);

                    //Get the element in ini format
                    if (checkBox2.Checked)
                        device_list_ini.Add(get_ic_string_ini(manufacturers[i], devices[k], ref devstruct) + Environment.NewLine);

                    //Get the element in C header format
                    if (checkBox1.Checked)
                        device_list_c.Add(get_ic_string_c(manufacturers[i], devices[k], ref devstruct));

                    //Get the element in xml format
                    if (checkBox3.Checked)
                    {
                        device_list_xml.Add(get_ic_xml(manufacturers[i], devices[k], ref devstruct));
                    }
                    total_count++;
                }
                progressBar.Value += 1;
                Application.DoEvents();
            }
            progressBar.Value = progressBar.Maximum;
            try
            {
                //Write the devices.h file
                if (checkBox1.Checked)
                {
                    using (StreamWriter stream_writer = new StreamWriter("devices.h"))
                    {
                        foreach (string elem in device_list_c)
                            stream_writer.WriteLine(elem);
                    }
                }

                //write the devices.ini file
                if (checkBox2.Checked)
                {
                    using (StreamWriter stream_writer = new StreamWriter("devices.ini"))
                    {
                        foreach (string elem in device_list_ini)
                            stream_writer.WriteLine(elem);
                    }
                }

                //write the devices.xml file
                if (checkBox3.Checked)
                {
                    XmlTextWriter xml_text_writer = new XmlTextWriter("Devices.xml", System.Text.Encoding.UTF8);
                    xml_text_writer.Formatting = Formatting.Indented;
                    xml_text_writer.Indentation = 2;
                    XmlSerializer serializer = new XmlSerializer(device_list_xml.GetType(), new XmlRootAttribute("devices"));
                    XmlSerializerNamespaces serializer_namespace = new XmlSerializerNamespaces();
                    serializer_namespace.Add("", "");
                    serializer.Serialize(xml_text_writer, device_list_xml, serializer_namespace);
                    xml_text_writer.Close();
                }

                //write the log.txt file
                if (checkBox4.Checked)
                {
                    using (StreamWriter stream_writer = new StreamWriter("log.txt"))
                    {
                        foreach (KeyValuePair<uint, string> key in total)
                        {
                            stream_writer.WriteLine("Protocol:0x" + key.Key.ToString("X2") + Environment.NewLine + key.Value);
                        }
                        stream_writer.Write(Environment.NewLine +
                            total_count.ToString() + " devices in " +
                            total.Count.ToString() + " protocols.");

                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Save error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                progressBar.Value = 0;
                return;
            }
            MessageBox.Show(this, "Dump was saved in " + Application.StartupPath, "InfoIc", MessageBoxButtons.OK, MessageBoxIcon.Information);
            progressBar.Value = 0;
        }

        //return a bitmap from resource id.
        private Bitmap GetBitmapFromResources(uint resourcesID)
        {
            IntPtr Hmodule = LoadLibrary("InfoIc.dll");
            Bitmap bmp = null;
            IntPtr hBitmap = default(IntPtr);

            try
            {
                if (!Hmodule.Equals(IntPtr.Zero))
                {
                    hBitmap = LoadBitmap(Hmodule, (int)resourcesID);
                    if (!hBitmap.Equals(IntPtr.Zero))
                    {
                        bmp = Bitmap.FromHbitmap(hBitmap);
                    }
                    DeleteObject(hBitmap);
                    FreeLibrary(Hmodule);
                    return bmp;
                }

            }
            catch
            {
            }
            finally
            {
                if (!hBitmap.Equals(null))
                {
                    DeleteObject(hBitmap);
                }
            }
            return bmp;
        }

        //resize bitmap
        private Bitmap ResizeBitmap(Bitmap sourceBMP, int width, int height)
        {
            Bitmap result = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(result))
            {
                g.DrawImage(sourceBMP, 0, 0, width, height);
            }
            return result;
        }

    }
}

