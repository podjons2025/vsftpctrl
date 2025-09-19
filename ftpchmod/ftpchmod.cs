using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ftpchmod
{

    public partial class ftpchmod : Form
    {
        [DllImport("user32.dll")]
        public static extern int MessageBoxTimeoutA(IntPtr hWnd, string msg, string Caps, int type, int Id, int time);

        List<string> TempList = new List<string>();

        private string textTemp = "";
        public ftpchmod()
        {
            InitializeComponent();

            toolTip1.SetToolTip(button1, "增加账号目录，可以给同一个目录增加多个账号！！！");
            toolTip1.SetToolTip(button2, "查看目录名称，权限，密码等！！！");
            toolTip1.SetToolTip(button3, "修改权限、备注、账号、密码，可以单个修改，也可以同时一起修改！！！");
            toolTip1.SetToolTip(button4, "删除的账号对应的目录如果为空，将连同目录一起删除！！！");
            toolTip1.SetToolTip(button5, "发送vsftpd重启服务生效！！！");
            toolTip1.SetToolTip(label1, "目录名称区分大小写！！！");
            groupBox1.Enabled = false;
            //if (SshConnTest())
            //{
            //    GetOMR();
            //}
        }

        private bool SshConnTest()
        {
            Assembly asm = Assembly.Load(Properties.Resources.Renci_SshNet);
            Type type = asm.GetType("Renci.SshNet.SshClient");
            ConstructorInfo constructor = type.GetConstructor(new Type[] { typeof(string), typeof(int), typeof(string), typeof(string) });
            using (dynamic instance = Activator.CreateInstance(type, textBox5.Text, Convert.ToInt32(textBox6.Text), textBox7.Text, textBox8.Text))
            {
                try
                {
                    instance.Connect();
                }
                catch { }
                if (instance.IsConnected)
                {
                    groupBox2.Enabled = false;
                    //button7.Enabled = false;
                    button7.Text = "登录成功";
                    return true;
                }
                else
                {
                    button7.Text = "登录主机";
                    groupBox1.Enabled = false;
                    return false;
                }

            }
        }

        private void GetOMR()
        {
            string resline = "cat /etc/vsftpd/virtusers";
            string res = SshConn(resline);
            string[] lines = res.Split('\n');
            for (int n = 0; n < lines.Length; n++)
            {
                if (!string.IsNullOrWhiteSpace(lines[n]))
                {
                    TempList.Add(lines[n]);
                }

            }
            //listBox1.Items.Add("账号："+"\t"+"|"+"目录：");
            for (int i = 0; i < TempList.Count; i += 2)
            {
                listBox1.Items.Add(TempList[i]);
            }

        }
        private string SshConn(string cmdline)
        {
            Assembly asm = Assembly.Load(Properties.Resources.Renci_SshNet);
            Type type = asm.GetType("Renci.SshNet.SshClient");
            ConstructorInfo constructor = type.GetConstructor(new Type[] { typeof(string), typeof(int), typeof(string), typeof(string) });
            using (dynamic instance = Activator.CreateInstance(type, textBox5.Text, Convert.ToInt32(textBox6.Text), textBox7.Text, textBox8.Text))
            {
                instance.Connect();
                string result = "";
                using (var cmd = instance.CreateCommand(cmdline))
                {
                    result = cmd.Execute();//获取返回值
                }
                instance.Disconnect();
                return result;
            }

        }
        private void textBoxDeny(object sender, EventArgs e)
        {
            //将sender转换为TextBox类型
            TextBox textBox = sender as TextBox;
            //如果转换成功
            if (textBox != null)
            {
                //使用正则表达式来匹配和删除非法字符
                Regex reg = new Regex(@"[\u4e00-\u9fa5]+|[^a-zA-Z0-9]");
                if (reg.IsMatch(textBox.Text))
                {
                    //记录删除前的文本长度
                    int oldLength = textBox.Text.Length;
                    //删除非法字符
                    textBox.Text = reg.Replace(textBox.Text, "");
                    //将光标移动到末尾
                    textBox.SelectionStart = textBox.Text.Length;
                    //如果文本长度发生变化，说明有非法字符被删除
                    if (textBox.Text.Length != oldLength)
                    {
                        //弹出提示信息
                        MessageBoxTimeoutA((IntPtr)0, "不能输入中文字符或特殊字符！" + Environment.NewLine, "vsftpd管理工具", 16, 0, 2000);
                    }
                }
            }
        }

        private string[] RadioButton_Check()
        {
            string[] lines = { };

            if (!string.IsNullOrWhiteSpace(textBox1.Text))
            {
                if (textBox1.Modified) textTemp = textBox1.Text; //保持textBox1.Text传来的大小写不变
            }

            if (radioButton1.Checked)
            {
                //1.虚拟用户具有写权限（上传、下载、删除、重命名）。
                lines = new string[] { "#0" + " " + textBox2.Text, "local_root=/data/virtual/ftp-data/ftp-21port/" +  textTemp, "virtual_use_local_privs=YES", "write_enable=YES" };
            }
            if (radioButton2.Checked)
            {
                //2.虚拟用户不能浏览目录，只能上传文件，无其他权限。
                lines = new string[] { "#1" + " " + textBox2.Text, "local_root=/data/virtual/ftp-data/ftp-21port/" + textTemp, "virtual_use_local_privs=NO", "write_enable=YES", "anon_world_readable_only=YES", "anon_upload_enable=YES" };
            }
            if (radioButton3.Checked)
            {
                //3.用户只能上传文件，新建，浏览目录，其它无权限。
                lines = new string[] { "#2" + " " + textBox2.Text, "local_root=/data/virtual/ftp-data/ftp-21port/" + textTemp, "virtual_use_local_privs=NO", "anon_world_readable_only=NO", "anon_other_write_enable=NO", "download_enable=NO", "anon_upload_enable=YES", "anon_mkdir_write_enable=YES", "dirlist_enable=YES" };
            }
            if (radioButton4.Checked)
            {
                //4.虚拟用户只能下载文件，无其他权限。
                lines = new string[] { "#3" + " " + textBox2.Text, "local_root=/data/virtual/ftp-data/ftp-21port/" + textTemp, "virtual_use_local_privs=NO", "write_enable=YES", "anon_world_readable_only=NO", "anon_upload_enable=NO" };
            }
            if (radioButton5.Checked)
            {
                //5.虚拟用户只能上传和下载文件，无其他权限。
                lines = new string[] { "#4" + " " + textBox2.Text, "local_root=/data/virtual/ftp-data/ftp-21port/" + textTemp, "virtual_use_local_privs=NO", "write_enable=YES", "anon_world_readable_only=NO", "anon_upload_enable=YES" };
            }
            if (radioButton6.Checked)
            {
                //6.虚拟用户只能下载文件和创建文件夹，无其他权限。
                lines = new string[] { "#5" + " " + textBox2.Text, "local_root=/data/virtual/ftp-data/ftp-21port/" + textTemp, "virtual_use_local_privs=NO", "write_enable=YES", "anon_world_readable_only=NO", "anon_mkdir_write_enable=YES" };
            }
            if (radioButton7.Checked)
            {
                //7.虚拟用户只能下载、删除和重命名文件，无其他权限。
                lines = new string[] { "#6" + " " + textBox2.Text, "local_root=/data/virtual/ftp-data/ftp-21port/" + textTemp, "virtual_use_local_privs=NO", "write_enable=YES", "anon_world_readable_only=NO", "anon_other_write_enable=YES" };
            }
            textTemp = "";
            return lines;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (RadioButton_Check().Length != 0)
            {
                if (!string.IsNullOrWhiteSpace(textBox1.Text))
                {
                    if (!string.IsNullOrWhiteSpace(textBox2.Text))
                    {
                        if (!string.IsNullOrWhiteSpace(textBox3.Text))
                        {
                            if (!string.IsNullOrWhiteSpace(textBox4.Text))
                            {
                                bool selectNO = false;
                                string strline = "/etc/vsftpd/user_conf/" + textBox3.Text;
                                Assembly asm = Assembly.Load(Properties.Resources.Renci_SshNet);
                                Type type = asm.GetType("Renci.SshNet.SftpClient");
                                ConstructorInfo constructor = type.GetConstructor(new Type[] { typeof(string), typeof(int), typeof(string), typeof(string) });
                                using (dynamic instance = Activator.CreateInstance(type, textBox5.Text, Convert.ToInt32(textBox6.Text), textBox7.Text, textBox8.Text))
                                {
                                    instance.Connect();
                                    List<string> itemList_new = new List<string>();
                                    string nameline = "/etc/vsftpd/virtusers";
                                    if (!instance.Exists(nameline))
                                    {
                                        instance.CreateText(nameline,UTF8Encoding.UTF8);
                                    }
                                    string[] TempList_new = instance.ReadAllLines(nameline);
                                    foreach (string line_new in TempList_new)
                                    {
                                        itemList_new.Add(line_new);
                                    }
                                    listBox1.Refresh();
                                    listBox1.Update();

                                    if (itemList_new.Contains(textBox3.Text))
                                    {
                                        int index_add_tmp = itemList_new.IndexOf(textBox3.Text); // 查找第一个索引
                                        int index_add = index_add_tmp + 1;
                                        if (index_add >= 0 && index_add % 2 != 0) // 索引大于等于0且为奇数
                                        {
                                            MessageBoxTimeoutA((IntPtr)0, "新增的账号已存在列表中,请更改其它账号！！！" + Environment.NewLine, "vsftpd管理工具", 16, 0, 3000);
                                        }
                                        else
                                        {
                                            if (instance.Exists("/data/virtual/ftp-data/ftp-21port/" + textBox1.Text))
                                            {
                                                if (MessageBox.Show("输入的目录已经存在！！" + Environment.NewLine + Environment.NewLine + "是否继续在此目录增加额外的账号？", "vsftpd管理工具", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                                                {
                                                    selectNO = true;
                                                    if (instance.Exists(strline))
                                                    {
                                                        instance.DeleteFile(strline);
                                                        instance.WriteAllLines(strline, RadioButton_Check(), UTF8Encoding.UTF8);
                                                    }
                                                    else
                                                    {
                                                        instance.WriteAllLines(strline, RadioButton_Check(), UTF8Encoding.UTF8);
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                selectNO = true;
                                                if (instance.Exists(strline))
                                                {
                                                    instance.DeleteFile(strline);
                                                    instance.WriteAllLines(strline, RadioButton_Check(), UTF8Encoding.UTF8);
                                                }
                                                else
                                                {
                                                    instance.WriteAllLines(strline, RadioButton_Check(), UTF8Encoding.UTF8);
                                                }
                                                //SshConn("mkdir" + " " + "/data/virtual/ftp-data/ftp-21port/" + textBox1.Text);
                                                instance.CreateDirectory("/data/virtual/ftp-data/ftp-21port/" + textBox1.Text);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (instance.Exists("/data/virtual/ftp-data/ftp-21port/" + textBox1.Text))
                                        {
                                            if (MessageBox.Show("输入的目录已经存在！！" + Environment.NewLine + Environment.NewLine + "是否继续在此目录增加额外的账号？", "vsftpd管理工具", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                                            {
                                                selectNO = true;
                                                if (instance.Exists(strline))
                                                {
                                                    instance.DeleteFile(strline);
                                                    instance.WriteAllLines(strline, RadioButton_Check(), UTF8Encoding.UTF8);
                                                }
                                                else
                                                {
                                                    instance.WriteAllLines(strline, RadioButton_Check(), UTF8Encoding.UTF8);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            selectNO = true;
                                            if (instance.Exists(strline))
                                            {
                                                instance.DeleteFile(strline);
                                                instance.WriteAllLines(strline, RadioButton_Check(), UTF8Encoding.UTF8);
                                            }
                                            else
                                            {
                                                instance.WriteAllLines(strline, RadioButton_Check(), UTF8Encoding.UTF8);
                                            }
                                            //SshConn("mkdir" + " " + "/data/virtual/ftp-data/ftp-21port/" + textBox1.Text);
                                            instance.CreateDirectory("/data/virtual/ftp-data/ftp-21port/" + textBox1.Text);
                                        }
                                    }  
                                    instance.Disconnect();
                                }

                                if (selectNO)
                                {
                                    SshConn("dos2unix" + " " + strline + " "
                                            + "&&" + " " + "echo" + " " + textBox3.Text + " " + ">> /etc/vsftpd/virtusers" + " "
                                            + "&&" + " " + "echo" + " " + textBox4.Text + " " + ">> /etc/vsftpd/virtusers" + " "
                                            + "&&" + " " + "dos2unix /etc/vsftpd/virtusers" + " "
                                            + "&&" + " " + "chown" + " " + "ftp.ftp" + " " + "/data/virtual/ftp-data/ftp-21port/" + textBox1.Text + " "
                                            + "&&" + " " + "rm -rf /etc/vsftpd/virtusers.db" + " "
                                            + "&&" + " " + "db_load -T -t hash -f /etc/vsftpd/virtusers /etc/vsftpd/virtusers.db"
                                            );
                                    textBox1.Clear();
                                    textBox2.Clear();
                                    textBox3.Clear();
                                    textBox4.Clear();
                                    radioButton_Checked_False();
                                    listBox1.ClearSelected();
                                    listBox1.Items.Clear();
                                    TempList.Clear();
                                    GetOMR();
                                    listBox1.Refresh();
                                    listBox1.Update();
                                    MessageBoxTimeoutA((IntPtr)0, "新建账号完成！" + Environment.NewLine, "vsftpd管理工具", 64, 0, 3000);
                                }
                                
                            }

                            else
                            {
                                MessageBoxTimeoutA((IntPtr)0, "请输入密码！" + Environment.NewLine, "vsftpd管理工具", 16, 0, 3000);
                            }
                        }
                        else
                        {
                            MessageBoxTimeoutA((IntPtr)0, "请输入账号！" + Environment.NewLine, "vsftpd管理工具", 16, 0, 3000);
                        }
                    }
                    else
                    {
                        MessageBoxTimeoutA((IntPtr)0, "请输入中文备注内容！" + Environment.NewLine, "vsftpd管理工具", 16, 0, 3000);
                    }

                }
                else
                {
                    MessageBoxTimeoutA((IntPtr)0, "请输入目录！" + Environment.NewLine, "vsftpd管理工具", 16, 0, 3000);
                }
            }
            else
            {
                MessageBoxTimeoutA((IntPtr)0, "请选择用户权限！" + Environment.NewLine, "vsftpd管理工具", 16, 0, 3000);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            string[] readerline = { "1.用户具有写权限（上传、下载、删除、重命名)",
                "2.用户不能浏览目录，只能上传文件，无其他权限",
                "3.用户只能上传文件，新建，浏览目录，其它无权限",
                "4.用户只能下载文件，无其他权限",
                "5.用户只能上传和下载文件，无其他权限",
                "6.用户只能下载文件和创建文件夹，无其他权限",
                "7.用户只能下载、删除和重命名文件，无其他权限"
            };

            int count = listBox1.SelectedItems.Count;//判断选中了几个
            List<string> itemValues = new List<string>();

            if (count == 0)
            {
                //如果所选项个数为0，则进行提示
                MessageBoxTimeoutA((IntPtr)0, "请选择需要查看的账号！" + Environment.NewLine, "vsftpd管理工具", 16, 0, 3000);
            }
            else
            {
                //将选中项的内容添加到字符串列表中
                for (int i = 0; i < count; i++)
                {
                    itemValues.Add(listBox1.SelectedItems[i].ToString().Trim());
                }

                //按照内容从列表框中查看的数据

                foreach (string item in itemValues)
                {
                    string password = "";
                    int index_pwd = TempList.IndexOf(item) + 1;
                    if (index_pwd >= 0 && index_pwd % 2 != 0) // 索引大于等于0且为奇数
                    {
                        password = TempList[index_pwd].Trim();  //奇数
                    }
                    else
                    {
                        password = TempList[index_pwd + 1].Trim();  //偶数
                    }

                    string cline = "/etc/vsftpd/user_conf/" + item;
                    Assembly asm = Assembly.Load(Properties.Resources.Renci_SshNet);
                    Type type = asm.GetType("Renci.SshNet.SftpClient");
                    ConstructorInfo constructor = type.GetConstructor(new Type[] { typeof(string), typeof(int), typeof(string), typeof(string) });
                    using (dynamic instance = Activator.CreateInstance(type, textBox5.Text, Convert.ToInt32(textBox6.Text), textBox7.Text, textBox8.Text))
                    {
                        instance.Connect();
                        if (instance.Exists(cline))
                        {
                            string result = "";
                            string regResult = "";
                            string[] lines = instance.ReadAllLines(cline);

                            int Numb = 0;
                            string firstLine = lines[0];
                            string NumbValue = @"\d+";
                            Match matchNumb = Regex.Match(firstLine, NumbValue);
                            if (matchNumb.Success)
                            {
                                Numb = int.Parse(matchNumb.Value);
                            }

                            int indexAfer = firstLine.IndexOf(" ");
                            regResult = firstLine.Substring(indexAfer + 1);
                            if (string.IsNullOrWhiteSpace(regResult))
                            {
                                string Regpattern = @"#\d+\s+(.*)";
                                Match Regmatch = Regex.Match(firstLine, Regpattern);
                                if (Regmatch.Success)
                                {
                                    regResult = Regmatch.Groups[1].Value;
                                }
                            }

                            string secondLine = lines[1];
                            string pattern = @".*/(.*)$"; //获取目录
                            Match match = Regex.Match(secondLine, pattern); //获取目录
                            string patternValue = @"(?<=ftp-)\d+"; //获取端口
                            Match matchValue = Regex.Match(secondLine, patternValue); //获取端口
                            if (match.Success)
                            {
                                if (matchValue.Success)
                                {
                                    result = match.Groups[1].Value;
                                }
                            }
                            if (instance.Exists( "/data/virtual/ftp-data/ftp-21port/" + result) || result.Equals("ftp-21port"))
                            {
                                MessageBox.Show("使用端口：" + matchValue + Environment.NewLine +
                                                    "账号名称：" + item + Environment.NewLine +
                                                    "使用密码：" + password + Environment.NewLine +
                                                    "目录名称：" + result + Environment.NewLine +
                                                    "目录备注：" + regResult + Environment.NewLine +
                                                    "目录权限：" + readerline[Numb],
                                                    "vsftpd管理工具", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                            else
                            {
                                if (matchValue.ToString().Trim() != "21")
                                {
                                    MessageBox.Show("使用端口：" + matchValue + Environment.NewLine +
                                                    "账号名称：" + item + Environment.NewLine +
                                                    "使用密码：" + password + Environment.NewLine +
                                                    "目录名称：" + result + Environment.NewLine +
                                                    "目录备注：" + regResult + Environment.NewLine +
                                                    "目录权限：" + readerline[Numb],
                                                    "vsftpd管理工具", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                }
                                else
                                {
                                    MessageBox.Show("使用端口：" + matchValue + Environment.NewLine +
                                                    "账号名称：" + item + Environment.NewLine +
                                                    "使用密码：" + password + Environment.NewLine +
                                                    "目录名称：" + result + " 目录不存在，账号无效，请尝试删除" + Environment.NewLine +
                                                    "目录备注：" + regResult + Environment.NewLine +
                                                    "目录权限：" + readerline[Numb],
                                                    "vsftpd管理工具", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                }

                            }
                        }
                        else
                        {
                            MessageBoxTimeoutA((IntPtr)0, "该选中的账号无效，请尝试删除！" + Environment.NewLine, "vsftpd管理工具", 16, 0, 3000);
                        }
                        listBox1.Refresh();
                        listBox1.Update();
                        instance.Disconnect();
                    }


                }
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            int count = listBox1.SelectedItems.Count;//判断选中了几个
            List<string> itemValues = new List<string>();
            
            if (count == 0)
            {
                //如果所选项个数为0，则进行提示
                MessageBoxTimeoutA((IntPtr)0, "请选择需要修改的账号！" + Environment.NewLine, "vsftpd管理工具", 16, 0, 3000);
            }
            else
            {
                //将选中项的内容添加到字符串列表中
                for (int i = 0; i < count; i++)
                {
                    itemValues.Add(listBox1.SelectedItems[i].ToString().Trim());
                }

                //按照内容从列表框中查看的数据
                bool valuetemp_1 = false;
                bool valuetemp_2 = true;
                textBox1.Clear();
                foreach (string item in itemValues)
                {
                    Assembly asm = Assembly.Load(Properties.Resources.Renci_SshNet);
                    Type type = asm.GetType("Renci.SshNet.SftpClient");
                    ConstructorInfo constructor = type.GetConstructor(new Type[] { typeof(string), typeof(int), typeof(string), typeof(string) });
                    using (dynamic instance = Activator.CreateInstance(type, textBox5.Text, Convert.ToInt32(textBox6.Text), textBox7.Text, textBox8.Text))
                    {
                        instance.Connect();
                        string cline = "/etc/vsftpd/user_conf/" + item;
                        if (instance.Exists(cline))
                        {
                            if (RadioButton_Check().Length != 0)
                            {
                                //修改权限
                                string regResult = "";
                                string[] lines = instance.ReadAllLines(cline);
                                string firstLine = lines[0];
                                int indexAfer = firstLine.IndexOf(" ");
                                regResult = firstLine.Substring(indexAfer + 1);
                                if (string.IsNullOrWhiteSpace(regResult))
                                {
                                    string Regpattern = @"#\d+\s+(.*)";
                                    Match Regmatch = Regex.Match(firstLine, Regpattern);
                                    if (Regmatch.Success)
                                    {
                                        regResult = Regmatch.Groups[1].Value;
                                    }
                                }

                                string secondLine = lines[1];
                                string pattern = @".*/(.*)$"; //获取目录
                                Match match = Regex.Match(secondLine, pattern); //获取目录
                                if (match.Success)
                                {
                                    textTemp = match.Groups[1].Value;
                                }

                                string[] strLines = RadioButton_Check();
                                string RegpatTemp = @"#\d+";
                                Match RegmatTemp = Regex.Match(strLines[0], RegpatTemp);
                                strLines[0] = RegmatTemp.Value + " " + regResult;

                                instance.DeleteFile(cline);
                                instance.WriteAllLines(cline, strLines, UTF8Encoding.UTF8);

                                SshConn("dos2unix" + " " + cline);
                                valuetemp_1 = true;
                                Thread.Sleep(300);
                            }
                            if (!string.IsNullOrWhiteSpace(textBox2.Text))
                            {
                                //修改备注
                                string[] lines = instance.ReadAllLines(cline);
                                string Regpat = @"#\d+";
                                Match Regmat = Regex.Match(lines[0], Regpat);
                                //lines[0] += Regmatch.Value + " " + textBox2.Text; //从后面增加
                                lines[0] = Regmat.Value + " " + textBox2.Text; //覆盖写入
                                instance.WriteAllLines(cline, lines, UTF8Encoding.UTF8);
                                SshConn("dos2unix" + " " + cline);
                                textBox2.Clear();
                                valuetemp_1 = true;
                                Thread.Sleep(200);
                            }
                            if (!string.IsNullOrWhiteSpace(textBox3.Text) && !string.IsNullOrWhiteSpace(textBox4.Text))
                            {
                                //同时修改账号，密码
                                List<string> itemList_1 = new List<string>();
                                string[] TempList_1 = instance.ReadAllLines("/etc/vsftpd/virtusers");
                                foreach (string line_1 in TempList_1)
                                {
                                    itemList_1.Add(line_1);
                                }
                                listBox1.Refresh();
                                listBox1.Update();
                                if (itemList_1.Contains(item))
                                {
                                    if (itemList_1.Contains(textBox3.Text))
                                    {
                                        int index_A_tmp = itemList_1.IndexOf(textBox3.Text); // 查找第一个索引
                                        int index_A = index_A_tmp + 1;
                                        if (index_A >= 0 && index_A % 2 != 0) // 索引大于等于0且为奇数
                                        {
                                            valuetemp_2 = false;
                                            MessageBoxTimeoutA((IntPtr)0, "选中的账号与录入修改的存在列表中,请更改其它账号！！！" + Environment.NewLine, "vsftpd管理工具", 16, 0, 3000);
                                        }
                                    }
                                    else
                                    {
                                        int index_1 = itemList_1.IndexOf(item);
                                        itemList_1[index_1] = textBox3.Text;
                                        itemList_1[index_1 + 1] = textBox4.Text;
                                        string textFile_1 = string.Join(Environment.NewLine, itemList_1);
                                        instance.DeleteFile("/etc/vsftpd/virtusers");
                                        instance.WriteAllText("/etc/vsftpd/virtusers_tmp", textFile_1, UTF8Encoding.UTF8); //WriteAllText方法输出格式有错
                                        string[] tempList_1 = instance.ReadAllLines("/etc/vsftpd/virtusers_tmp");
                                        instance.WriteAllLines("/etc/vsftpd/virtusers", tempList_1, UTF8Encoding.UTF8);
                                        SshConn("dos2unix" + " " + "/etc/vsftpd/virtusers" + " "
                                                + "&&" + " " + "rm -rf /etc/vsftpd/virtusers_tmp" + " "
                                                + "&&" + " " + "rm -rf /etc/vsftpd/virtusers.db" + " "
                                                + "&&" + " " + "db_load -T -t hash -f /etc/vsftpd/virtusers /etc/vsftpd/virtusers.db" + " "
                                                + "&&" + " " + "mv" + " " + cline + " " + "/etc/vsftpd/user_conf/" + textBox3.Text
                                                );
                                        textBox3.Clear();
                                        textBox4.Clear();
                                        listBox1.ClearSelected();
                                        listBox1.Items.Clear();
                                        TempList.Clear();
                                        itemList_1.Clear();
                                        GetOMR();
                                        valuetemp_1 = true;
                                        Thread.Sleep(200);
                                    }
                                }
                                else
                                {
                                    MessageBoxTimeoutA((IntPtr)0, "该选中的账号无效！！重新运行程序，再尝试！！" + Environment.NewLine, "vsftpd管理工具", 16, 0, 3000);
                                }

                            }
                            else
                            {
                                if (!string.IsNullOrWhiteSpace(textBox3.Text))
                                {
                                    //修改账号
                                    List<string> itemList_2 = new List<string>();
                                    string[] TempList_2 = instance.ReadAllLines("/etc/vsftpd/virtusers");
                                    foreach (string line_2 in TempList_2)
                                    {
                                        itemList_2.Add(line_2);
                                    }
                                    listBox1.Refresh();
                                    listBox1.Update();
                                    if (itemList_2.Contains(item))
                                    {
                                        if (itemList_2.Contains(textBox3.Text))
                                        {
                                            int index_B_tmp = itemList_2.IndexOf(textBox3.Text); // 查找第一个索引
                                            int index_B = index_B_tmp + 1;
                                            if (index_B >= 0 && index_B % 2 != 0) // 索引大于等于0且为奇数
                                            {
                                                valuetemp_2 = false;
                                                MessageBoxTimeoutA((IntPtr)0, "选中的账号与录入修改的存在列表中,请更改其它账号！！！" + Environment.NewLine, "vsftpd管理工具", 16, 0, 3000);
                                            }
                                            else
                                            {
                                                int index_2 = itemList_2.IndexOf(item);
                                                itemList_2[index_2] = textBox3.Text;
                                                string textFile_2 = string.Join(Environment.NewLine, itemList_2);
                                                instance.DeleteFile("/etc/vsftpd/virtusers");
                                                instance.WriteAllText("/etc/vsftpd/virtusers_tmp", textFile_2, UTF8Encoding.UTF8); //WriteAllText方法输出格式有错
                                                string[] tempList_2 = instance.ReadAllLines("/etc/vsftpd/virtusers_tmp");
                                                instance.WriteAllLines("/etc/vsftpd/virtusers", tempList_2, UTF8Encoding.UTF8);
                                                SshConn("dos2unix" + " " + "/etc/vsftpd/virtusers" + " "
                                                        + "&&" + " " + "rm -rf /etc/vsftpd/virtusers_tmp" + " "
                                                        + "&&" + " " + "rm -rf /etc/vsftpd/virtusers.db" + " "
                                                        + "&&" + " " + "db_load -T -t hash -f /etc/vsftpd/virtusers /etc/vsftpd/virtusers.db" + " "
                                                        + "&&" + " " + "mv" + " " + cline + " " + "/etc/vsftpd/user_conf/" + textBox3.Text
                                                        );
                                                textBox3.Clear();
                                                listBox1.ClearSelected();
                                                listBox1.Items.Clear();
                                                TempList.Clear();
                                                itemList_2.Clear();
                                                GetOMR();
                                                valuetemp_1 = true;
                                                Thread.Sleep(200);
                                            }
                                        }
                                        else
                                        {
                                            int index_2 = itemList_2.IndexOf(item);
                                            itemList_2[index_2] = textBox3.Text;
                                            string textFile_2 = string.Join(Environment.NewLine, itemList_2);
                                            instance.DeleteFile("/etc/vsftpd/virtusers");
                                            instance.WriteAllText("/etc/vsftpd/virtusers_tmp", textFile_2, UTF8Encoding.UTF8); //WriteAllText方法输出格式有错
                                            string[] tempList_2 = instance.ReadAllLines("/etc/vsftpd/virtusers_tmp");
                                            instance.WriteAllLines("/etc/vsftpd/virtusers", tempList_2, UTF8Encoding.UTF8);
                                            SshConn("dos2unix" + " " + "/etc/vsftpd/virtusers" + " "
                                                    + "&&" + " " + "rm -rf /etc/vsftpd/virtusers_tmp" + " "
                                                    + "&&" + " " + "rm -rf /etc/vsftpd/virtusers.db" + " "
                                                    + "&&" + " " + "db_load -T -t hash -f /etc/vsftpd/virtusers /etc/vsftpd/virtusers.db" + " "
                                                    + "&&" + " " + "mv" + " " + cline + " " + "/etc/vsftpd/user_conf/" + textBox3.Text
                                                    );
                                            textBox3.Clear();
                                            listBox1.ClearSelected();
                                            listBox1.Items.Clear();
                                            TempList.Clear();
                                            itemList_2.Clear();
                                            GetOMR();
                                            valuetemp_1 = true;
                                            Thread.Sleep(200);
                                        }

                                    }
                                    else
                                    {
                                        MessageBoxTimeoutA((IntPtr)0, "该选中的账号无效！！重新运行程序，再尝试！！" + Environment.NewLine, "vsftpd管理工具", 16, 0, 3000);
                                    }

                                }
                                if (!string.IsNullOrWhiteSpace(textBox4.Text))
                                {
                                    //修改密码
                                    List<string> itemList_3 = new List<string>();
                                    string[] TempList_3 = instance.ReadAllLines("/etc/vsftpd/virtusers");
                                    foreach (string line_3 in TempList_3)
                                    {
                                        itemList_3.Add(line_3);
                                    }
                                    listBox1.Refresh();
                                    listBox1.Update();
                                    int index_3 = itemList_3.IndexOf(item) + 1;
                                    itemList_3[index_3] = textBox4.Text;
                                    string textFile_3 = string.Join(Environment.NewLine, itemList_3);
                                    instance.DeleteFile("/etc/vsftpd/virtusers");
                                    instance.WriteAllText("/etc/vsftpd/virtusers_tmp", textFile_3, UTF8Encoding.UTF8); //WriteAllText方法输出格式有错
                                    string[] tempList_3 = instance.ReadAllLines("/etc/vsftpd/virtusers_tmp");
                                    instance.WriteAllLines("/etc/vsftpd/virtusers", tempList_3, UTF8Encoding.UTF8);
                                    SshConn("dos2unix" + " " + "/etc/vsftpd/virtusers" + " "
                                            + "&&" + " " + "rm -rf /etc/vsftpd/virtusers_tmp" + " "
                                            + "&&" + " " + "rm -rf /etc/vsftpd/virtusers.db" + " "
                                            + "&&" + " " + "db_load -T -t hash -f /etc/vsftpd/virtusers /etc/vsftpd/virtusers.db"
                                            );
                                    textBox4.Clear();
                                    listBox1.ClearSelected();
                                    listBox1.Items.Clear();
                                    TempList.Clear();
                                    itemList_3.Clear();
                                    GetOMR();
                                    valuetemp_1 = true;
                                    Thread.Sleep(200);
                                }
                            }

                        }
                        else
                        {
                            MessageBoxTimeoutA((IntPtr)0, "该选中的账号无效，请尝试删除掉！" + Environment.NewLine, "vsftpd管理工具", 16, 0, 3000);
                        }
                      
                        instance.Disconnect();
                    }

                }
                if (valuetemp_1)
                {
                    radioButton_Checked_False();
                    MessageBoxTimeoutA((IntPtr)0, "修改完成！" + Environment.NewLine, "vsftpd管理工具", 64, 0, 3000);
                }
                else
                {
                    if (valuetemp_2)
                    {
                        textBox1.Clear();
                        radioButton_Checked_False();
                        MessageBoxTimeoutA((IntPtr)0, "请选择权限或输入要修改的内容！" + Environment.NewLine, "vsftpd管理工具", 16, 0, 3000);
                    }

                }
            }
        }

        private void radioButton_Checked_False()
        {
            radioButton1.Checked = false;
            radioButton2.Checked = false;
            radioButton3.Checked = false;
            radioButton4.Checked = false;
            radioButton5.Checked = false;
            radioButton6.Checked = false;
            radioButton7.Checked = false;
            listBox1.ClearSelected();
        }

        private void groupBox1_Click(object sender, EventArgs e)
        {
            radioButton_Checked_False();
        }


        private void groupBox2_Click(object sender, EventArgs e)
        {
            radioButton_Checked_False();
        }

        private void ftpchmod_Click(object sender, EventArgs e)
        {
            radioButton_Checked_False();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            int count = listBox1.SelectedItems.Count;//判断选中了几个
            List<string> itemValues = new List<string>();
            List<string> itemList = new List<string>();

            if (count == 0)
            {
                //如果所选项个数为0，则进行提示
                MessageBoxTimeoutA((IntPtr)0, "请选择需要删除的账号！" + Environment.NewLine, "vsftpd管理工具", 16, 0, 3000);
            }
            else
            {
                //将选中项的内容添加到字符串列表中
                for (int i = 0; i < count; i++)
                {
                    itemValues.Add(listBox1.SelectedItems[i].ToString().Trim());
                }

                //按照内容从列表框中查看的数据
                foreach (string item in itemValues)
                {
                    Assembly asm = Assembly.Load(Properties.Resources.Renci_SshNet);
                    Type type = asm.GetType("Renci.SshNet.SftpClient");
                    ConstructorInfo constructor = type.GetConstructor(new Type[] { typeof(string), typeof(int), typeof(string), typeof(string) });
                    using (dynamic instance = Activator.CreateInstance(type, textBox5.Text, Convert.ToInt32(textBox6.Text), textBox7.Text, textBox8.Text))
                    {
                        instance.Connect();

                        string cline = "/etc/vsftpd/user_conf/" + item;
                        string delPath = "";

                        if (instance.Exists(cline))
                        {
                            string[] lines = instance.ReadAllLines(cline);
                            string secondLine = lines[1];
                            string pattern = @".*/(.*)$"; //获取目录
                            Match match = Regex.Match(secondLine, pattern); //获取目录
                            string folderTemp = match.Groups[1].Value;
                            delPath = "/data/virtual/ftp-data/ftp-21port/" + folderTemp;
                            instance.DeleteFile(cline);
                        }

                        if (instance.Exists("/etc/vsftpd/virtusers"))
                        {
                            string[] TempList_Del = instance.ReadAllLines("/etc/vsftpd/virtusers");
                            foreach (string line in TempList_Del)
                            {
                                itemList.Add(line);
                            }
                           // int index = Array.IndexOf(tempList, item);
                            int index_del = itemList.IndexOf(item);
                            listBox1.Items.Remove(item);
                            itemList.RemoveAt(index_del);
                            itemList.RemoveAt(index_del);
                            instance.DeleteFile("/etc/vsftpd/virtusers");
                            instance.WriteAllLines("/etc/vsftpd/virtusers", itemList, UTF8Encoding.UTF8);
                        }
                        
                        if (!string.IsNullOrWhiteSpace(delPath) && instance.Exists(delPath))
                        {
                            string resEmpty = SshConn("ls -A" + " " + delPath);
                            if (string.IsNullOrWhiteSpace(resEmpty))
                            {
                                instance.DeleteDirectory(delPath);
                            }
                        }
                        listBox1.Refresh();
                        listBox1.Update();
                        instance.Disconnect();
                    }
                }
                SshConn("dos2unix" + " " + "/etc/vsftpd/virtusers" + " "
                        + "&&" + " " + "rm -rf /etc/vsftpd/virtusers.db" + " "
                        + "&&" + " " + "db_load -T -t hash -f /etc/vsftpd/virtusers /etc/vsftpd/virtusers.db"
                        );
                radioButton_Checked_False();
                MessageBoxTimeoutA((IntPtr)0, "删除账号完成！" + Environment.NewLine, "vsftpd管理工具", 64, 0, 3000);
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            SshConn("systemctl restart vsftpd21");
            MessageBoxTimeoutA((IntPtr)0, "发送vsftpd重启服务完成！" + Environment.NewLine, "vsftpd管理工具", 64, 0, 3000);
        }

        protected override bool ProcessCmdKey(ref System.Windows.Forms.Message msg, System.Windows.Forms.Keys keyData) //激活回车键
        {
            int WM_KEYDOWN = 256;
            int WM_SYSKEYDOWN = 260;
            if (msg.Msg == WM_KEYDOWN | msg.Msg == WM_SYSKEYDOWN)
            {
                switch (keyData)
                {
                    case Keys.Escape:
                        this.Close();//esc关闭窗体
                        break;
                }
            }
            return false;
        }

        private void button7_Click(object sender, EventArgs e)
        {
            if (SshConnTest())
            {
                groupBox1.Enabled = true;
                GetOMR();
            }
            else
            {
                MessageBox.Show("输入错误，请检查，继续尝试！！！", "vsftpd管理工具", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void textBox5_Clicked(object sender, EventArgs e)
        {
            textBox5.Clear();
        }

        private void textBox6_Clicked(object sender, EventArgs e)
        {
            textBox6.Clear();
        }

        private void textBox7_Clicked(object sender, EventArgs e)
        {
            textBox7.Clear();
        }

        private void textBox8_Clicked(object sender, EventArgs e)
        {
            textBox8.Clear();
        }

    }
}
