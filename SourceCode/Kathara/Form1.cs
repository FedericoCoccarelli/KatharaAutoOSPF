using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace Kathara
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        string filePath;
        Dictionary<string, int> deviceInterfaces;
        private void button1_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                if (openFileDialog1.FileName.Contains("lab.conf"))
                {
                    richTextBox1.Text = openFileDialog1.FileName;
                    filePath = richTextBox1.Text;
                    richTextBox1.ReadOnly = true;
                }
                else
                {
                    MessageBox.Show("Choose a file named 'lab.conf'", "lab.conf not found", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (filePath == null)
            {
                ;
                MessageBox.Show("Choose a file named 'lab.conf'", "lab.conf not found", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            Dictionary<string, List<string>> deviceConnections = GetDeviceConnections(filePath);
            FillDataGridView(deviceConnections);
        }

        private void FillDataGridView(Dictionary<string, List<string>> deviceConnections)
        {
            dataGridView1.ColumnCount = 2;
            dataGridView1.Columns[0].Name = "Device";
            dataGridView1.Columns[0].ReadOnly = true;
            dataGridView1.Columns[1].Name = "Interface";
            dataGridView1.Columns[1].ReadOnly = true;

            foreach (var item in deviceConnections)
            {
                string deviceName = item.Key;
                List<string> connectedDevices = item.Value;

                for (int i = 0; i < connectedDevices.Count; i++)
                {
                    string interfaceName = "eth" + i;
                    dataGridView1.Rows.Add(deviceName, interfaceName, "");
                }
            }
            dataGridView1.AllowUserToAddRows = false;

            string fileName = "config.txt";
            string filePathText = Path.Combine(Path.GetDirectoryName(filePath), fileName);

            if (File.Exists(filePathText))
            {
                MessageBox.Show("Il file esiste già.", "Avviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                using (StreamWriter writer = new StreamWriter(filePathText))
                {
                    for (int i = 0; i <= dataGridView1.Rows.Count - 1; i++)
                    {
                        for (int j = 0; j < dataGridView1.Columns.Count; j++)
                        {
                            string cellValue = dataGridView1.Rows[i].Cells[j].Value?.ToString();
                            writer.Write(cellValue + "\t");
                        }
                        writer.WriteLine();
                    }
                }
            }
        }



        static Dictionary<string, List<string>> GetDeviceConnections(string filePath)
        {
            Dictionary<string, List<string>> deviceConnections = new Dictionary<string, List<string>>();

            using (StreamReader reader = new StreamReader(filePath))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.Contains("="))
                    {
                        int startIndex = line.IndexOf("[");
                        int endIndex = line.IndexOf("]");
                        if (startIndex != -1 && endIndex != -1)
                        {
                            string device = line.Substring(0, startIndex);
                            string connectedDevice = line.Substring(startIndex + 1, endIndex - startIndex - 1);

                            if (!deviceConnections.ContainsKey(device))
                            {
                                deviceConnections[device] = new List<string>();
                            }
                            deviceConnections[device].Add(connectedDevice);
                        }
                    }
                }
            }
            return deviceConnections;
        }
        private void button3_Click(object sender, EventArgs e)
        {
            if (filePath==null)
            {;
                MessageBox.Show("Choose a file named 'lab.conf'", "lab.conf not found", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            string file = Path.Combine(Path.GetDirectoryName(filePath), "config.txt");
            if (File.Exists(file))
            {
                string[] lines = File.ReadAllLines(file);
                // Create list of devices
                List<string> devices= new List<string> { };
                try
                {
                    devices = lines.Select(line => line.Split(new char[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)[0]).Distinct().ToList();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Each line must contain at least 6 elements (device,interface,ip,subnet,mask,area)", "Not enough elements on at least a line", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                dataGridView1.Rows.Clear();

                //control on lines elements
                foreach (string devic in devices)
                {
                    List<string> deviceLines = lines.Where(line => line.StartsWith(devic)).ToList();
                    foreach (string deviceLine in deviceLines)
                    {
                        string[] elements = deviceLine.Split(new char[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        if (elements.Count()<6)
                        {
                            MessageBox.Show("Each line must contain at least 6 elements", "Not enough elements on at least a line", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            return;
                        }
                        

                        dataGridView1.ColumnCount = 6;
                        dataGridView1.Columns[0].Name = "Device";
                        dataGridView1.Columns[0].ReadOnly = true;
                        dataGridView1.Columns[1].Name = "Interface";
                        dataGridView1.Columns[1].ReadOnly = true;
                        dataGridView1.Columns[2].Name = "IP";
                        dataGridView1.Columns[2].ReadOnly = true;
                        dataGridView1.Columns[3].Name = "Subnet";
                        dataGridView1.Columns[3].ReadOnly = true;
                        dataGridView1.Columns[4].Name = "Mask";
                        dataGridView1.Columns[4].ReadOnly = true;
                        dataGridView1.Columns[5].Name = "Area";
                        dataGridView1.Columns[5].ReadOnly = true;

                        dataGridView1.Rows.Add(elements);

                    }
                    
                }
                
                // Loop through each device
                foreach (string device in devices)
                {
                    // Get all lines for current device
                    List<string> deviceLines = lines.Where(line => line.StartsWith(device)).ToList();

                    if (device.StartsWith("r"))
                    {
                        // Create directory for device
                        string directory = Path.Combine(Path.GetDirectoryName(filePath), device);
                        Directory.CreateDirectory(Path.Combine(directory, "etc", "quagga"));
                        Directory.CreateDirectory(Path.Combine(directory, "etc", "init.d"));
                        Directory.CreateDirectory(Path.Combine(directory, "etc", "network"));

                        string ospfd = Path.Combine(directory, "etc", "quagga", "ospfd.conf");
                        string zebra = Path.Combine(directory, "etc", "quagga", "zebra.conf");
                        string daemons = Path.Combine(directory, "etc", "quagga", "daemons");
                        string networkingRestart = Path.Combine(directory, "etc", "init.d", "networking restart");
                        string quaggaRestart = Path.Combine(directory, "etc", "init.d", "quagga restart");
                        string interfaces = Path.Combine(directory, "etc", "network", "interfaces");

                        // Create OSPF configuration file
                        using (StreamWriter writer = new StreamWriter(ospfd))
                        {
                            writer.WriteLine("hostname " + device);
                            writer.WriteLine("password zebra");
                            writer.WriteLine("");

                            // Write all interfaces
                            foreach (string deviceLine in deviceLines)
                            {

                                string[] elements = deviceLine.Split(new char[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                                string interfaceName = elements[1];

                                writer.WriteLine("interface " + interfaceName);
                                writer.WriteLine("ospf hello-interval 2");
                                writer.WriteLine("");
                            }

                            writer.WriteLine("router ospf");

                            // Write all IP addresses
                            foreach (string deviceLine in deviceLines)
                            {

                                string[] elements = deviceLine.Split(new char[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                                string ipAddress = elements[2];
                                string subnet = elements[3];
                                string mask = elements[4];
                                string area = elements[5];

                                // Check if IP address is not empty
                                if (!string.IsNullOrEmpty(subnet))
                                {
                                    writer.WriteLine("network " + subnet + mask + " area " + area);
                                }
                            }
                        }

                        // Create zebra file
                        using (StreamWriter writer = new StreamWriter(zebra))
                        {
                            writer.WriteLine("hostname " + device);
                            writer.WriteLine("password zebra");
                            writer.WriteLine("enable password zebra");
                            writer.WriteLine("");

                            // Write all IP addresses
                            foreach (string deviceLine in deviceLines)
                            {

                                string[] elements = deviceLine.Split(new char[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                                string interfaceName = elements[1];
                                string ipAddress = elements[2];
                                string subnet = elements[3];
                                string mask = elements[4];
                                string area = elements[5];

                                // Check if IP address is not empty
                                if (!string.IsNullOrEmpty(subnet))
                                {
                                    writer.WriteLine("interface " + interfaceName);
                                    writer.WriteLine("ip address " + ipAddress + mask);
                                    writer.WriteLine("link-detect");
                                    writer.WriteLine("");
                                }
                            }
                        }

                        // Create daemons file
                        using (StreamWriter writer = new StreamWriter(daemons))
                        {
                            writer.WriteLine("zebra=yes\r\nbgpd=no\r\nospfd=yes\r\nospf6d=no\r\nripd=no\r\nripngd=no\r\nisisd=no\r\nldpd=no\r\n");
                        }

                        // Create quaggarestart file
                        using (StreamWriter writer = new StreamWriter(quaggaRestart))
                        {

                        }

                        // Create networkingrestart file
                        using (StreamWriter writer = new StreamWriter(networkingRestart))
                        {

                        }

                        // Create interfaces file
                        using (StreamWriter writer = new StreamWriter(interfaces))
                        {
                            foreach (string deviceLine in deviceLines)
                            {
                                if (device.StartsWith("r"))
                                {
                                    string[] elements = deviceLine.Split(new char[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                                    string interfaceName = elements[1];
                                    string ipAddress = elements[2];
                                    string subnet = elements[3];
                                    string mask = elements[4];
                                    string area = elements[5];

                                    writer.WriteLine("auto " + interfaceName);
                                    writer.WriteLine("iface " + interfaceName + " inet static");
                                    writer.WriteLine("\taddress " + ipAddress+mask);
                                    writer.WriteLine("");
                                }
                            }
                        }

                        //create startup file
                        string startup = device + ".startup";
                        startup = Path.Combine(Path.GetDirectoryName(filePath), startup);
                        using (StreamWriter writer = new StreamWriter( startup))
                        {
                            writer.WriteLine("/etc/init.d/networking restart");
                            writer.WriteLine("/etc/init.d/quagga restart");
                        }
                    }
                    else
                    {
                        string directory = Path.Combine(Path.GetDirectoryName(filePath), device);
                        Directory.CreateDirectory(Path.Combine(directory, "etc", "init.d"));
                        Directory.CreateDirectory(Path.Combine(directory, "etc", "network"));

                        string networkingRestart = Path.Combine(directory, "etc", "init.d", "networking restart");
                        string interfaces = Path.Combine(directory, "etc", "network", "interfaces");

                        // Create networkingrestart file
                        using (StreamWriter writer = new StreamWriter(networkingRestart))
                        {
                           
                        }

                        // Create interfaces file
                        using (StreamWriter writer = new StreamWriter(interfaces))
                        {
                            foreach (string deviceLine in deviceLines)
                            {
                                if (!device.StartsWith("r"))
                                {
                                    string[] elements = deviceLine.Split(new char[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                                    string interfaceName = elements[1];
                                    string ipAddress = elements[2];
                                    string mask = elements[4];
                                    string[] parts = ipAddress.Split('.');
                                    parts[3] = "1";
                                    string gateway = String.Join(".", parts); ;

                                    writer.WriteLine("auto " + interfaceName);
                                    writer.WriteLine("iface " + interfaceName + " inet static");
                                    writer.WriteLine("\taddress " + ipAddress + mask);

                                    foreach (string deviceLinee in deviceLines)
                                    {
                                        string[] elementss = deviceLine.Split(new char[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                                        if (elementss[0].StartsWith("r") && elementss[3] == elements[3]) { gateway = elementss[2]; richTextBox1.Text += elementss[0]; }
                                    }

                                    writer.WriteLine("\tgateway " + gateway);
                                    writer.WriteLine("");
                                }
                            }
                        }

                        //create startup file
                        string startup = device + ".startup";
                        startup = Path.Combine(Path.GetDirectoryName(filePath), startup);
                        using (StreamWriter writer = new StreamWriter(startup))
                        {
                            writer.WriteLine("/etc/init.d/networking restart");
                        }
                    }

                }
            }
        }
    }
}
