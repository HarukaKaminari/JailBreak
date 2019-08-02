using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace txt2bin
{
    public partial class formMain : Form
    {
        private List<Byte> m_ContentsByte = new List<Byte>();

        public formMain()
        {
            InitializeComponent();
        }
        
        private void btnOpenTxt_Click(object sender, EventArgs e)
        {
            CDG1.Title = "Open *.txt";
            CDG1.DefaultExt = "txt";
            CDG1.Filter = "*.txt|*.txt";
            CDG1.Multiselect = true;
            DialogResult result = CDG1.ShowDialog();
            if (result != DialogResult.OK && result != DialogResult.Yes)
                return;

            // 文件名排序，保证数据先后顺序正确
            List<string> fileNames = CDG1.FileNames.ToList();
            fileNames.Sort();
            
            // 所有文件内容合并
            string contentTxt = string.Empty;
            foreach(string fileName in fileNames)
            {
                StreamReader sr = new StreamReader(fileName);
                if (sr != null)
                {
                    contentTxt += sr.ReadToEnd();
                    
                    sr.Close();
                    sr.Dispose();
                    sr = null;
                }
            }

            // 开始解码
            if (chkIsOld.Checked)
            {
                string[] bytesStr = contentTxt.Split(' ');
                foreach(string b in bytesStr)
                {
                    if(b.Length > 0)
                        m_ContentsByte.Add(byte.Parse(b, System.Globalization.NumberStyles.AllowHexSpecifier));
                }
            }
            else
            {
                char[] chars = contentTxt.ToArray();
                int offset = 0;
                int data = (chars[offset] - 0x20) | ((chars[offset + 1] - 0x20) << 6);
                offset++;
                int curBitCount = 12;
                while (true)
                {
                    if (curBitCount <= 0)
                        break;
                    Byte lower8bit = (Byte)(data & 0xFF);
                    m_ContentsByte.Add(lower8bit);
                    data >>= 8;
                    curBitCount -= 8;
                    if (curBitCount < 8)
                    {
                        offset++;
                        if (offset < chars.Length)
                        {
                            int newData = chars[offset] - 0x20;
                            data |= (newData << curBitCount);
                            curBitCount += 6;
                            // 这个时候有可能依然没凑够8位，再尝试取一次
                            if (curBitCount < 8)
                            {
                                offset++;
                                if (offset < chars.Length)
                                {
                                    int newData2 = chars[offset] - 0x20;
                                    data |= (newData2 << curBitCount);
                                    curBitCount += 6;
                                }
                            }
                        }
                    }
                }
            }
        }

        private void btnSaveBin_Click(object sender, EventArgs e)
        {
            CDG2.Title = "Save as *.*";
            CDG1.DefaultExt = "";
            CDG1.Filter = "*.*|*.*";
            DialogResult result = CDG2.ShowDialog();
            if (result != DialogResult.OK && result != DialogResult.Yes)
                return;

            FileStream fs = new FileStream(CDG2.FileName, FileMode.Create);
            if (fs != null)
            {
                BinaryWriter bw = new BinaryWriter(fs);
                if (bw != null)
                {
                    bw.Write(m_ContentsByte.ToArray());

                    bw.Flush();
                    bw.Close();
                    bw = null;
                }
                fs.Close();
                fs.Dispose();
                fs = null;
            }
        }

        private void btnOpenBin_Click(object sender, EventArgs e)
        {
            CDG1.Title = "Open *.*";
            CDG1.DefaultExt = "";
            CDG1.Filter = "*.*|*.*";
            CDG1.Multiselect = false;
            DialogResult result = CDG1.ShowDialog();
            if (result != DialogResult.OK && result != DialogResult.Yes)
                return;

            FileStream fs = new FileStream(CDG1.FileName, FileMode.Open);
            if(fs != null)
            {
                BinaryReader br = new BinaryReader(fs);
                if(br != null)
                {
                    m_ContentsByte = br.ReadBytes((int)fs.Length).ToList();

                    processContentsByte();

                    br.Close();
                    br.Dispose();
                    br = null;
                }
                fs.Close();
                fs.Dispose();
                fs = null;
            }
        }

        private void btnSaveTxt_Click(object sender, EventArgs e)
        {
            CDG2.Title = "Save as *.txt";
            CDG2.DefaultExt = "txt";
            CDG2.Filter = "*.txt|*.txt";
            DialogResult result = CDG2.ShowDialog();
            if (result != DialogResult.OK && result != DialogResult.Yes)
                return;

            List<List<char>> ret = processContentsByte();
            for(int i = 0; i < ret.Count; ++i)
            {
                string fileName = CDG2.FileName.Substring(0, CDG2.FileName.Length - 4) + string.Format("{0:D4}", i) + ".txt";
                StreamWriter sw = new StreamWriter(fileName);
                if (sw != null)
                {
                    sw.Write(ret[i].ToArray());
                    sw.Flush();
                    sw.Close();
                    sw.Dispose();
                    sw = null;
                }
            }
        }

        private List<List<char>> processContentsByte()
        {
            List<List<char>> ret = new List<List<char>>();
            int capacity = int.Parse(txtBankSize.Text);

            int offset = 0;
            int data = (m_ContentsByte[offset + 1] << 8) | m_ContentsByte[offset];
            offset++;
            int curBitCount = 16;
            List<char> container = new List<char>();
            while (true)
            {
                // 如果全部数据都处理完了，则退出循环
                if (curBitCount <= 0)
                    break;
                // 取出最低6位
                Byte lower6bit = (Byte)(data & 0x3F);
                // 转成字符并写入流
                char c = (char)(lower6bit + 0x20);
                container.Add(c);
                // 容器写满了，存入列表，并准备新容器
                if(capacity > 0 && container.Count >= capacity)
                {
                    ret.Add(container);
                    container = new List<char>();
                }
                // 剩下的数据向右移6位
                data >>= 6;
                curBitCount -= 6;
                // 如果剩下的数据位数不够6位，则读下一个字节填补进来
                if (curBitCount < 6)
                {
                    // 准备下一个字节
                    offset++;
                    // 如果未读完全部数据，读下一个字节并填充
                    if (offset < m_ContentsByte.Count)
                    {
                        int newData = m_ContentsByte[offset];
                        data |= (newData << curBitCount);
                        curBitCount += 8;
                    }
                }
            }
            // 最后再将剩下的一个未填满的容器存入列表
            // 如果文件体积十分巧合地正好是容量的整数倍，则这一次存入列表的容器将会是空容器。不过这无所谓，保存的时候空容器不会造成任何问题
            ret.Add(container);

            return ret;
        }

        private void chkIsOld_CheckedChanged(object sender, EventArgs e)
        {

        }
    }
}
