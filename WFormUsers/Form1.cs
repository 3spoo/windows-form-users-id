using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WFormUsers
{
    public partial class Form1 : Form
    {
        List<Index> INDICI = new List<Index>();

        const string FILE_USERS = "data.dat";
        const string FILE_INDICI = "id.dat";

        const int DIM_USERNAME = 8;
        const int DIM_PASSSWORD = 25;

        // username (long = 8) + password (unicode: 2 byte per char) + isActive (1)
        const int DIM_RECORD = DIM_USERNAME + (DIM_PASSSWORD) * 2 + 1;

        // username (8) + posizione (8)
        const int DIM_ID = DIM_USERNAME + 8;

        public class User
        {
            private long _username;

            public long Username
            {
                get { return _username; }
                set { _username = value; }
            }

            private string _password;

            public string Password
            {
                get { return _password; }
                set { _password = value; }
            }

            private bool _isActive;

            public bool IsActive
            {
                get { return _isActive; }
                set { _isActive = value; }
            }

            public User(long username, string password)
            {
                _username = username;
                _password = password;
                _isActive = true;
            }

            public User(long username, string password, bool active)
            {
                _username = username;
                _password = password;
                _isActive = active;
            }
        }

        public class Index
        {
            private long _username;

            public long Username
            {
                get { return _username; }
                set { _username = value; }
            }

            private long _position;

            public long Position
            {
                get { return _position; }
                set { _position = value; }
            }

            public Index(long username, long position)
            {
                _username = username;
                _position = position;
            }
        }

        public Form1()
        {
            InitializeComponent();

            CreateEnvironment();
            RefreshGrid();
        }

        public void CreateEnvironment()
        {
            using (FileStream fs = new FileStream(FILE_USERS, FileMode.OpenOrCreate))
            {
                if (fs.Length % DIM_RECORD != 0)
                {
                    MessageBox.Show("FILE NON VALIDO", "ERROR");
                    return;
                }
            }

            using (FileStream fs = new FileStream(FILE_INDICI, FileMode.OpenOrCreate))
            {
                if (fs.Length % DIM_ID != 0)
                {
                    MessageBox.Show("FILE NON VALIDO", "ERROR");
                    return;
                }

                for (long j = 0; j * DIM_ID < fs.Length; j++)
                {
                    fs.Seek(j * DIM_ID, SeekOrigin.Begin);
                    Index tmp_id = ReadId(fs);

                    if (tmp_id != null)
                        INDICI.Add(tmp_id);
                }
            }
        }

        // RICARICA LA GRIGLIA DAL FILE (NON DEVE ESSERE CHIAMATA DENTRO UNO USING SU FILE_USERS)
        void RefreshGrid()
        {
            dataGridView1.Rows.Clear();

            using (FileStream fs = new FileStream(FILE_USERS, FileMode.OpenOrCreate))
            {
                foreach (Index tmp_id in INDICI)
                {
                    User tmp_user = ReadUser(fs, tmp_id.Position);

                    if (tmp_user != null && tmp_user.IsActive)
                        dataGridView1.Rows.Add(tmp_user.Username, tmp_user.Password);
                }
            }
        }

        // SALVA L'INDICE SU FILE (SOVRASCRITTURA INTERA)
        void SaveIndexes()
        {
            using (FileStream fs = new FileStream(FILE_INDICI, FileMode.Create))
            {
                WriteIds(fs, INDICI);
            }
        }

        public int BinarySearchByUsername(List<Index> ids, long username)
        {
            int start = 0;
            int end = ids.Count - 1;

            while (start <= end)
            {
                int mid = (start + end) / 2;
                long tmp_username = ids[mid].Username;

                if (tmp_username == username)
                    return mid;

                if (tmp_username < username)
                    start = mid + 1;
                else
                    end = mid - 1;
            }

            return -1;
        }

        public Index ReadId(FileStream fs)
        {
            try
            {
                using (BinaryReader br = new BinaryReader(fs, Encoding.Unicode, true))
                {
                    long username = br.ReadInt64();
                    long position = br.ReadInt64();

                    return new Index(username, position);
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        public void WriteIds(FileStream fs, List<Index> ids)
        {
            using (BinaryWriter bw = new BinaryWriter(fs, Encoding.Unicode, true))
            {
                foreach (Index tmp_id in ids)
                {
                    bw.Write(tmp_id.Username);
                    bw.Write(tmp_id.Position);
                }
            }
        }

        public User ReadUser(FileStream fs, long position)
        {
            try
            {
                fs.Seek(position * DIM_RECORD, SeekOrigin.Begin);

                using (BinaryReader br = new BinaryReader(fs, Encoding.Unicode, true))
                {
                    long username = br.ReadInt64();
                    string password = ReadString(br, DIM_PASSSWORD);
                    bool is_active = br.ReadBoolean();

                    return new User(username, password, is_active);
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        public void WriteUser(FileStream fs, User user, long position)
        {
            fs.Seek(position * DIM_RECORD, SeekOrigin.Begin);

            using (BinaryWriter bw = new BinaryWriter(fs, Encoding.Unicode, true))
            {
                bw.Write(user.Username);
                WriteString(bw, user.Password, DIM_PASSSWORD);
                bw.Write(user.IsActive);
            }
        }

        public string ReadString(BinaryReader br, int length)
        {
            byte[] data = br.ReadBytes(length * 2);
            return Encoding.Unicode.GetString(data).TrimEnd('\0', ' ');
        }

        public void WriteString(BinaryWriter bw, string string_to_write, int length)
        {
            string_to_write = string_to_write.PadRight(length).Substring(0, length);
            byte[] binary_string = Encoding.Unicode.GetBytes(string_to_write);

            bw.Write(binary_string);
        }

        // ------------------------------------------------------------
        // METODI DI SUPPORTO
        // ------------------------------------------------------------

        // VALIDA LO USERNAME (5 CIFRE) E LO CONVERTE
        bool TryReadUsername(out long username)
        {
            username = 0;
            string s = textBox1.Text.Trim();

            if (s.Length != 5 || !s.All(char.IsDigit) || !long.TryParse(s, out username))
            {
                MessageBox.Show("USERNAME NON VALIDO, DEVE ESSERE DI 5 CIFRE.", "ERROR");
                return false;
            }

            return true;
        }

        bool TryReadPassword(out string password)
        {
            password = textBox2.Text.Trim();

            if (password.Length < 1)
            {
                MessageBox.Show("PASSWORD NON VALIDA", "ERROR");
                return false;
            }

            return true;
        }

        void ResetFields()
        {
            textBox1.Text = "";
            textBox2.Text = "";
        }

        // ------------------------------------------------------------
        // EVENTI
        // button1 = INSERISCI, button2 = CERCA, button3 = MODIFICA, button4 = ELIMINA
        // ------------------------------------------------------------

        private void button1_Click(object sender, EventArgs e)
        {
            if (!TryReadUsername(out long username))
                return;

            if (!TryReadPassword(out string password))
                return;

            try
            {
                User user = new User(username, password);
                int i = BinarySearchByUsername(INDICI, username);

                using (FileStream fs = new FileStream(FILE_USERS, FileMode.OpenOrCreate))
                {
                    if (i != -1)
                    {
                        // USERNAME GIA PRESENTE NELL'INDICE
                        long position = INDICI[i].Position;
                        User existing = ReadUser(fs, position);

                        if (existing != null && existing.IsActive)
                        {
                            MessageBox.Show("UTENTE GIA PRESENTE NEL DATA_BASE", "ERROR");
                            return;
                        }

                        // UTENTE CANCELLATO: SOVRASCRITTURA DEL RECORD
                        WriteUser(fs, user, position);
                    }
                    else
                    {
                        // NUOVO USERNAME: INSERIMENTO IN CODA
                        long position = fs.Length / DIM_RECORD;
                        WriteUser(fs, user, position);

                        INDICI.Add(new Index(username, position));
                        INDICI.Sort((a, b) => a.Username.CompareTo(b.Username));
                    }
                }

                SaveIndexes();
                RefreshGrid();
                ResetFields();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "ERROR");
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (!TryReadUsername(out long username))
                return;

            try
            {
                int i = BinarySearchByUsername(INDICI, username);
                if (i == -1)
                {
                    MessageBox.Show("UTENTE NON TROVATO", "ERROR");
                    return;
                }

                using (FileStream fs = new FileStream(FILE_USERS, FileMode.Open))
                {
                    User user = ReadUser(fs, INDICI[i].Position);

                    if (user == null || !user.IsActive)
                    {
                        MessageBox.Show("UTENTE NON TROVATO", "ERROR");
                        return;
                    }

                    textBox1.Text = user.Username.ToString();
                    textBox2.Text = user.Password;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "ERROR");
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (!TryReadUsername(out long username))
                return;

            try
            {
                int i = BinarySearchByUsername(INDICI, username);
                if (i == -1)
                {
                    MessageBox.Show("UTENTE NON TROVATO", "ERROR");
                    return;
                }

                long position = INDICI[i].Position;

                using (FileStream fs = new FileStream(FILE_USERS, FileMode.Open))
                {
                    User user = ReadUser(fs, position);

                    if (user == null || !user.IsActive)
                    {
                        MessageBox.Show("UTENTE NON TROVATO", "ERROR");
                        return;
                    }

                    if (!TryReadPassword(out string password))
                        return;

                    user.Password = password;
                    WriteUser(fs, user, position);
                } // IL FILE VIENE CHIUSO QUI

                RefreshGrid();
                ResetFields();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "ERROR");
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (!TryReadUsername(out long username))
                return;

            try
            {
                int i = BinarySearchByUsername(INDICI, username);
                if (i == -1)
                {
                    MessageBox.Show("UTENTE NON TROVATO", "ERROR");
                    return;
                }

                long position = INDICI[i].Position;

                using (FileStream fs = new FileStream(FILE_USERS, FileMode.Open))
                {
                    User user = ReadUser(fs, position);

                    if (user == null || !user.IsActive)
                    {
                        MessageBox.Show("UTENTE NON TROVATO", "ERROR");
                        return;
                    }

                    // CANCELLAZIONE LOGICA
                    user.IsActive = false;
                    WriteUser(fs, user, position);
                } // IL FILE VIENE CHIUSO QUI

                RefreshGrid();
                ResetFields();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "ERROR");
            }
        }
    }
}