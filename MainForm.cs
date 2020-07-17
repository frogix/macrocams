using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace myCamViewer
{
    public partial class MainForm : Form
    {
        private CameraList camList = new CameraList();

        // Server configuration URL
        private readonly string configUrl = "http://demo.macroscop.com:8080/configex?login=root";

        // Object that limits number of threads that can access pictureBox to 1
        private object lockObj = new object();

        // Token to stop video translation 
        private static CancellationTokenSource cts = new CancellationTokenSource();

        private SynchronizationContext sync = new SynchronizationContext();

        public MainForm()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            UpdateCamList();

            if (camList.Count != 0)
                listBox1.SelectedIndex = 0;
        }

        void updatePB (Image img)
        {
            // update image on main thread
            sync.Post(new SendOrPostCallback(_ => pictureBox1.Image = img), null);
        }

        private void UpdateCamList()
        {
            camList = extractCamerasFromXml(loadXmlDocument(configUrl));

            listBox1.Items.Clear();

            foreach (var cam in camList)
            {
                listBox1.Items.Add(cam.Name);
            }

           
        }

        /// <summary>
        /// Exctracts list of cameras from given Xml document
        /// </summary>
        /// <param name="doc">Xml document that has info about video channels</param>
        /// <returns>CameraList object</returns>
        private CameraList extractCamerasFromXml(XmlDocument doc)
        {
            CameraList cl = new CameraList();

            var els = doc.SelectNodes("Configuration/Channels/ChannelInfo");

            foreach (XmlNode el in els)
            {
                var xmlEl = (XmlElement)el;

                string name = xmlEl.GetAttribute("Name");
                string id   = xmlEl.GetAttribute("Id");

                Camera cam = new Camera(id, name);
                cam.Url = $"http://demo.macroscop.com:8080/mobile?login=root&channelid={id}&resolutionX=640&resolutionY=480&fps=25";

                cl.Add(cam);

            }

            return cl;
        }

        /// <summary>
        /// Loads XML document from given URL
        /// </summary>
        /// <param name="url">Url string</param>
        /// <returns></returns>
        private XmlDocument loadXmlDocument(string url)
        {
            string xmlStr;
            using (var wc = new WebClient())
            {
                xmlStr = wc.DownloadString(url);
            }
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xmlStr);

            return xmlDoc;

        }

        private void updCamListBtn_Click(object sender, EventArgs e)
        {
            UpdateCamList();
        }

        void ChangeCamera (string name)
        {
            // stop active video translation
            cts.Cancel();

            // create new cancellation token
            cts = new CancellationTokenSource();
            CancellationToken token = cts.Token;

            // get connection url
            string url = camList.GetByName(name).Url;

            // start new video translation
            Task.Run(() => VideoDecoder.StartAsync(updatePB, url, token: token));
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            ChangeCamera(listBox1.SelectedItem.ToString());
        }

        private void MainForm_Shown(object sender, EventArgs e)
        {
            if (camList.Count == 0)
                MessageBox.Show("No cameras were loaded :<");
            else
                MessageBox.Show($"Successfully loaded {camList.Count} cameras!");
        }
    }
}
