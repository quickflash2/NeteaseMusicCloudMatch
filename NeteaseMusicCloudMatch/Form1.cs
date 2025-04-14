using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;

namespace NeteaseMusicCloudMatch
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        public static string unikey = string.Empty, wyCookie = string.Empty, userId = string.Empty;

        private void Form1_Load(object sender, EventArgs e)
        {
            string LoginCheck = CommonHelper.Read("NeteaseMusic", "LoginCheck");
            if (!string.IsNullOrWhiteSpace(LoginCheck))
            {
                checkBox1.Checked = Convert.ToBoolean(LoginCheck);
            }

            if (checkBox1.Checked)
            {
                wyCookie = CommonHelper.Read("NeteaseMusic", "Cookie");
                if (!string.IsNullOrEmpty(wyCookie))
                {
                    LoadUIDName();
                    LoadCloudInfo();
                    // 移除自动刷新 button2_Click(sender, null);
                    timer1.Enabled = false;
                }
                else
                {
                    LoadQrCodeImage();
                }
            }
            else
            {
                LoadQrCodeImage();
            }

            LoadDgvColumns();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            CommonHelper.Write("NeteaseMusic", "LoginCheck", checkBox1.Checked.ToString());
        }

        #region dataGridView1 加载标题
        private void LoadDgvColumns()
        {
            dataGridView1.RowHeadersVisible = false;
            DataGridViewTextBoxColumn colListId = new DataGridViewTextBoxColumn();
            colListId.Name = "colListId";
            colListId.HeaderText = "#";
            colListId.ReadOnly = true;

            DataGridViewTextBoxColumn colSongId = new DataGridViewTextBoxColumn();
            colSongId.Name = "colSongId";
            colSongId.HeaderText = "ID";
            colSongId.ReadOnly = true;

            DataGridViewTextBoxColumn colFileName = new DataGridViewTextBoxColumn();
            colFileName.Name = "colFileName";
            colFileName.HeaderText = "文件名称";
            colFileName.ReadOnly = true;

            DataGridViewTextBoxColumn colFileSize = new DataGridViewTextBoxColumn();
            colFileSize.Name = "colFileSize";
            colFileSize.HeaderText = "大小";
            colFileSize.ReadOnly = true;

            DataGridViewTextBoxColumn colAddTime = new DataGridViewTextBoxColumn();
            colAddTime.Name = "colAddTime";
            colAddTime.HeaderText = "上传时间";
            colAddTime.ReadOnly = true;

            dataGridView1.Columns.AddRange(
                new DataGridViewColumn[] {
                    colListId, colSongId, colFileName, colFileSize, colAddTime
                });

            dataGridView1.Columns[0].FillWeight = 5;
            dataGridView1.Columns[1].FillWeight = 15;
            dataGridView1.Columns[2].FillWeight = 30;
            dataGridView1.Columns[3].FillWeight = 10;
            dataGridView1.Columns[4].FillWeight = 20;
        }
        #endregion

        #region 加载二维码图片
        private void LoadQrCodeImage()
        {
            try
            {
                string apiUrl = "https://music.163.com/api/login/qrcode/unikey?type=1";
                string html = CommonHelper.GetHtml(apiUrl);
                if (CommonHelper.CheckJson(html))
                {
                    var json = JObject.Parse(html);
                    if (json["code"]?.ToString() == "200")
                    {
                        unikey = json["unikey"]?.ToString();
                        string QrCodeUrl = "https://music.163.com/login?codekey=" + unikey;
                        pictureBox1.Image = CommonHelper.QrCodeCreate(QrCodeUrl);
                    }
                    else
                    {
                        MessageBox.Show("生成二维码unikey出错", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    MessageBox.Show(html, "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                MessageBox.Show(ex.Message);
            }
        }
        #endregion

        #region 加载UID和Name
        private void LoadUIDName()
        {
            try
            {
                string apiUrl = "https://music.163.com/api/nuser/account/get";
                string html = CommonHelper.GetHtml(apiUrl, wyCookie);
                if (CommonHelper.CheckJson(html))
                {
                    var json = JObject.Parse(html);
                    if (json["code"]?.ToString() == "200")
                    {
                        userId = json["profile"]?["userId"]?.ToString();
                        string nickname = json["profile"]?["nickname"]?.ToString();
                        string avatarUrl = json["profile"]?["avatarUrl"]?.ToString();
                        label1.Text = "UID：" + userId + "，Name：" + nickname;
                        pictureBox1.Image = CommonHelper.GetImage(avatarUrl);
                    }
                }
                else
                {
                    MessageBox.Show(html, "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                MessageBox.Show(ex.Message);
            }
        }
        #endregion

        #region 加载音乐网盘信息
        private void LoadCloudInfo()
        {
            try
            {
                string apiUrl = "https://music.163.com/api/v1/cloud/get?limit=0";
                string html = CommonHelper.GetHtml(apiUrl, wyCookie);
                if (CommonHelper.CheckJson(html))
                {
                    var json = JObject.Parse(html);
                    if (json["code"]?.ToString() == "200")
                    {
                        string size = json["size"]?.ToString();
                        string maxSize = json["maxSize"]?.ToString();
                        size = CommonHelper.GetFileSize(Convert.ToInt64(size));
                        maxSize = CommonHelper.GetFileSize(Convert.ToInt64(maxSize));
                        label2.Text = "音乐云盘容量：" + size + "  /  " + maxSize;
                    }
                }
                else
                {
                    MessageBox.Show(html, "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                MessageBox.Show(ex.Message);
            }
        }
        #endregion

        #region 检测扫码状态 / 重新扫码登录
        private void timer1_Tick(object sender, EventArgs e)
        {
            string apiUrl = "https://music.163.com/api/login/qrcode/client/login?type=1&key=" + unikey;

            HttpHelper http = new HttpHelper();
            HttpItem item = new HttpItem()
            {
                URL = apiUrl,
                Method = "get",
                ContentType = "application/json;charset=UTF-8",
                Referer = apiUrl,
                ResultType = ResultType.String
            };
            HttpResult result = http.GetHtml(item);

            string html = result.Html;
            if (CommonHelper.CheckJson(html))
            {
                var json = JObject.Parse(html);
                string code = json["code"]?.ToString();
                string message = json["message"]?.ToString();
                if (code == "800")
                {
                    wyCookie = string.Empty;
                    LoadQrCodeImage();
                }
                else if (code == "803")
                {
                    wyCookie = result.Cookie.Replace(",", ";");
                    CommonHelper.Write("NeteaseMusic", "Cookie", wyCookie);

                    LoadUIDName();
                    LoadCloudInfo();
                    // 移除自动刷新 button2_Click(sender, null);

                    timer1.Enabled = false;
                }
                string messStr = code + ", " + message;
                Console.WriteLine(messStr);
            }
            else
            {
                MessageBox.Show(html, "", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("确定要重新扫码登录吗？", "", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
            {
                wyCookie = string.Empty;
                label1.Text = string.Empty;
                label2.Text = string.Empty;

                LoadQrCodeImage();

                timer1.Enabled = true;
            }
        }
        #endregion

        #region 读取音乐网盘内容
        private void button2_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(wyCookie))
            {
                pageIndex = 1;
                Thread thread = new Thread(GetCloudData);
                thread.Start();
            }
            else
            {
                MessageBox.Show("还没登录呢", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        int pageIndex = 1;
        private void GetCloudData()
        {
            try
            {
                if (pageIndex <= 1)
                {
                    this.Invoke(new MethodInvoker(delegate ()
                    {
                        dataGridView1.Rows.Clear();
                    }));
                }
                int limit = 200;
                string apiUrl = "https://music.163.com/api/v1/cloud/get?limit=" + limit + "&offset=" + (pageIndex - 1) * limit;
                string html = CommonHelper.GetHtml(apiUrl, wyCookie);
                if (CommonHelper.CheckJson(html))
                {
                    var json = JObject.Parse(html);
                    if (json["code"]?.ToString() == "200")
                    {
                        if (json["count"]?.Value<int>() > 0)
                        {
                            var jarr = JArray.Parse(json["data"]?.ToString());
                            for (int i = 0; i < jarr.Count; i++)
                            {
                                var j = JObject.Parse(jarr[i].ToString());
                                string songId = j["songId"]?.ToString();
                                string fileName = j["fileName"]?.ToString();
                                string fileSize = j["fileSize"]?.ToString();
                                string addTime = j["addTime"]?.ToString();
                                int index = 0;
                                this.Invoke(new MethodInvoker(delegate ()
                                {
                                    index = dataGridView1.Rows.Add();
                                    dataGridView1.Rows[index].Cells[0].Value = dataGridView1.Rows.Count;
                                    dataGridView1.Rows[index].Cells[1].Value = songId;
                                    dataGridView1.Rows[index].Cells[2].Value = fileName;
                                    dataGridView1.Rows[index].Cells[3].Value = CommonHelper.GetFileSize(Convert.ToInt64(fileSize));
                                    dataGridView1.Rows[index].Cells[4].Value = CommonHelper.UnixTimestampToDateTime(addTime);
                                }));
                            }
                        }
                    }
                }
                else
                {
                    MessageBox.Show(html, "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                MessageBox.Show(ex.Message);
            }
        }

        private void ScrollReader(object sender, ScrollEventArgs e)
        {
            if (e.NewValue + dataGridView1.DisplayedRowCount(false) >= dataGridView1.RowCount)
            {
                pageIndex++;
                Thread thread = new Thread(GetCloudData);
                thread.Start();
            }
        }
        #endregion

        // 其他方法保持不变...
    }
}
