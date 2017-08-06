using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace OverlaysMiniApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void buttonGo_Click(object sender, RoutedEventArgs e)
        {
            if (comboBoxCylinders.Text == "" || comboBoxSync.Text == "")
            {
                MessageBox.Show("Make a selection for NUMBER OF CYLINDERS and SYNC", "Oops!", MessageBoxButton.OK, MessageBoxImage.Hand);
            }
            else
            {
                if (comboBoxCylinders.Text == "1")
                {
                    Window1 w1 = new Window1();
                    w1.Show();
                }
                else if (comboBoxCylinders.Text == "2")
                {
                    Window2 w2 = new Window2();
                    string sync = comboBoxSync.Text;

                    if (textBox1.Text == sync)
                    {
                        w2.label1.Content = textBox1.Text;
                        w2.label.Content = textBox2.Text;
                    }
                    else if (textBox2.Text == sync)
                    {
                        w2.label1.Content = textBox2.Text;
                        w2.label.Content = textBox1.Text;
                    }
                    w2.Show();
                }
                else if (comboBoxCylinders.Text == "3")
                {
                    Window3 w3 = new Window3();
                    string sync = comboBoxSync.Text;

                    if (textBox1.Text == sync)
                    {
                        w3.label2.Content = textBox1.Text;
                        w3.label1.Content = textBox2.Text;
                        w3.label.Content = textBox3.Text;
                    }
                    else if (textBox2.Text == sync)
                    {
                        w3.label2.Content = textBox2.Text;
                        w3.label1.Content = textBox3.Text;
                        w3.label.Content = textBox1.Text;
                    }
                    else if (textBox3.Text == sync)
                    {
                        w3.label2.Content = textBox3.Text;
                        w3.label1.Content = textBox1.Text;
                        w3.label.Content = textBox2.Text;
                    }
                    w3.Show();
                }
                else if (comboBoxCylinders.Text == "4")
                {
                    Window4 w4 = new Window4();
                    string sync = comboBoxSync.Text;

                    if (textBox1.Text == sync)
                    {
                        w4.label3.Content = textBox1.Text;
                        w4.label2.Content = textBox2.Text;
                        w4.label1.Content = textBox3.Text;
                        w4.label.Content = textBox4.Text;
                    }
                    else if (textBox2.Text == sync)
                    {
                        w4.label3.Content = textBox2.Text;
                        w4.label2.Content = textBox3.Text;
                        w4.label1.Content = textBox4.Text;
                        w4.label.Content = textBox1.Text;
                    }
                    else if (textBox3.Text == sync)
                    {
                        w4.label3.Content = textBox3.Text;
                        w4.label2.Content = textBox4.Text;
                        w4.label1.Content = textBox1.Text;
                        w4.label.Content = textBox2.Text;
                    }
                    else if (textBox4.Text == sync)
                    {
                        w4.label3.Content = textBox4.Text;
                        w4.label2.Content = textBox1.Text;
                        w4.label1.Content = textBox2.Text;
                        w4.label.Content = textBox3.Text;
                    }
                    w4.Show();
                }
                else if (comboBoxCylinders.Text == "5")
                {
                    Window5 w5 = new Window5();
                    string sync = comboBoxSync.Text;

                    if (textBox1.Text == sync)
                    {
                        w5.label4.Content = textBox1.Text;
                        w5.label3.Content = textBox2.Text;
                        w5.label2.Content = textBox3.Text;
                        w5.label1.Content = textBox4.Text;
                        w5.label.Content = textBox5.Text;
                    }
                    else if (textBox2.Text == sync)
                    {
                        w5.label4.Content = textBox2.Text;
                        w5.label3.Content = textBox3.Text;
                        w5.label2.Content = textBox4.Text;
                        w5.label1.Content = textBox5.Text;
                        w5.label.Content = textBox1.Text;
                    }
                    else if (textBox3.Text == sync)
                    {
                        w5.label4.Content = textBox3.Text;
                        w5.label3.Content = textBox4.Text;
                        w5.label2.Content = textBox5.Text;
                        w5.label1.Content = textBox1.Text;
                        w5.label.Content = textBox2.Text;
                    }
                    else if (textBox4.Text == sync)
                    {
                        w5.label4.Content = textBox4.Text;
                        w5.label3.Content = textBox5.Text;
                        w5.label2.Content = textBox1.Text;
                        w5.label1.Content = textBox2.Text;
                        w5.label.Content = textBox3.Text;
                    }
                    else if (textBox5.Text == sync)
                    {
                        w5.label4.Content = textBox5.Text;
                        w5.label3.Content = textBox1.Text;
                        w5.label2.Content = textBox2.Text;
                        w5.label1.Content = textBox3.Text;
                        w5.label.Content = textBox4.Text;
                    }
                    w5.Show();
                }
                else if (comboBoxCylinders.Text == "6")
                {
                    Window6 w6 = new Window6();
                    string sync = comboBoxSync.Text;

                    if (textBox1.Text == sync)
                    {
                        w6.label5.Content = textBox1.Text;
                        w6.label4.Content = textBox2.Text;
                        w6.label3.Content = textBox3.Text;
                        w6.label2.Content = textBox4.Text;
                        w6.label1.Content = textBox5.Text;
                        w6.label.Content = textBox6.Text;
                    }
                    else if (textBox2.Text == sync)
                    {
                        w6.label5.Content = textBox2.Text;
                        w6.label4.Content = textBox3.Text;
                        w6.label3.Content = textBox4.Text;
                        w6.label2.Content = textBox5.Text;
                        w6.label1.Content = textBox6.Text;
                        w6.label.Content = textBox1.Text;
                    }
                    else if (textBox3.Text == sync)
                    {
                        w6.label5.Content = textBox3.Text;
                        w6.label4.Content = textBox4.Text;
                        w6.label3.Content = textBox5.Text;
                        w6.label2.Content = textBox6.Text;
                        w6.label1.Content = textBox1.Text;
                        w6.label.Content = textBox2.Text;
                    }
                    else if (textBox4.Text == sync)
                    {
                        w6.label5.Content = textBox4.Text;
                        w6.label4.Content = textBox5.Text;
                        w6.label3.Content = textBox6.Text;
                        w6.label2.Content = textBox1.Text;
                        w6.label1.Content = textBox2.Text;
                        w6.label.Content = textBox3.Text;
                    }
                    else if (textBox5.Text == sync)
                    {
                        w6.label5.Content = textBox5.Text;
                        w6.label4.Content = textBox6.Text;
                        w6.label3.Content = textBox1.Text;
                        w6.label2.Content = textBox2.Text;
                        w6.label1.Content = textBox3.Text;
                        w6.label.Content = textBox4.Text;
                    }
                    else if (textBox6.Text == sync)
                    {
                        w6.label5.Content = textBox6.Text;
                        w6.label4.Content = textBox1.Text;
                        w6.label3.Content = textBox2.Text;
                        w6.label2.Content = textBox3.Text;
                        w6.label1.Content = textBox4.Text;
                        w6.label.Content = textBox5.Text;
                    }
                    w6.Show();
                }
                else if (comboBoxCylinders.Text == "7")
                {
                    Window7 w7 = new Window7();
                    string sync = comboBoxSync.Text;

                    if (textBox1.Text == sync)
                    {
                        w7.label6.Content = textBox1.Text;
                        w7.label5.Content = textBox2.Text;
                        w7.label4.Content = textBox3.Text;
                        w7.label3.Content = textBox4.Text;
                        w7.label2.Content = textBox5.Text;
                        w7.label1.Content = textBox6.Text;
                        w7.label.Content = textBox7.Text;
                    }
                    else if (textBox2.Text == sync)
                    {
                        w7.label6.Content = textBox2.Text;
                        w7.label5.Content = textBox3.Text;
                        w7.label4.Content = textBox4.Text;
                        w7.label3.Content = textBox5.Text;
                        w7.label2.Content = textBox6.Text;
                        w7.label1.Content = textBox7.Text;
                        w7.label.Content = textBox1.Text;

                    }
                    else if (textBox3.Text == sync)
                    {
                        w7.label6.Content = textBox3.Text;
                        w7.label5.Content = textBox4.Text;
                        w7.label4.Content = textBox5.Text;
                        w7.label3.Content = textBox6.Text;
                        w7.label2.Content = textBox7.Text;
                        w7.label1.Content = textBox1.Text;
                        w7.label.Content = textBox2.Text;
                    }
                    else if (textBox4.Text == sync)
                    {
                        w7.label6.Content = textBox4.Text;
                        w7.label5.Content = textBox5.Text;
                        w7.label4.Content = textBox6.Text;
                        w7.label3.Content = textBox7.Text;
                        w7.label2.Content = textBox1.Text;
                        w7.label1.Content = textBox2.Text;
                        w7.label.Content = textBox3.Text;
                    }
                    else if (textBox5.Text == sync)
                    {
                        w7.label6.Content = textBox5.Text;
                        w7.label5.Content = textBox6.Text;
                        w7.label4.Content = textBox7.Text;
                        w7.label3.Content = textBox1.Text;
                        w7.label2.Content = textBox2.Text;
                        w7.label1.Content = textBox3.Text;
                        w7.label.Content = textBox4.Text;
                    }
                    else if (textBox6.Text == sync)
                    {
                        w7.label6.Content = textBox6.Text;
                        w7.label5.Content = textBox7.Text;
                        w7.label4.Content = textBox1.Text;
                        w7.label3.Content = textBox2.Text;
                        w7.label2.Content = textBox3.Text;
                        w7.label1.Content = textBox4.Text;
                        w7.label.Content = textBox5.Text;
                    }
                    else if (textBox7.Text == sync)
                    {
                        w7.label6.Content = textBox7.Text;
                        w7.label5.Content = textBox1.Text;
                        w7.label4.Content = textBox2.Text;
                        w7.label3.Content = textBox3.Text;
                        w7.label2.Content = textBox4.Text;
                        w7.label1.Content = textBox5.Text;
                        w7.label.Content = textBox6.Text;
                    }
                    w7.Show();
                }
                else if (comboBoxCylinders.Text == "8")
                {
                    Window8 w8 = new Window8();
                    string sync = comboBoxSync.Text;

                    if (textBox1.Text == sync)
                    {
                        w8.label7.Content = textBox1.Text;
                        w8.label6.Content = textBox2.Text;
                        w8.label5.Content = textBox3.Text;
                        w8.label4.Content = textBox4.Text;
                        w8.label3.Content = textBox5.Text;
                        w8.label2.Content = textBox6.Text;
                        w8.label1.Content = textBox7.Text;
                        w8.label.Content = textBox8.Text;

                    }
                    else if (textBox2.Text == sync)
                    {
                        w8.label7.Content = textBox2.Text;
                        w8.label6.Content = textBox3.Text;
                        w8.label5.Content = textBox4.Text;
                        w8.label4.Content = textBox5.Text;
                        w8.label3.Content = textBox6.Text;
                        w8.label2.Content = textBox7.Text;
                        w8.label1.Content = textBox8.Text;
                        w8.label.Content = textBox1.Text;
                    }
                    else if (textBox3.Text == sync)
                    {
                        w8.label7.Content = textBox3.Text;
                        w8.label6.Content = textBox4.Text;
                        w8.label5.Content = textBox5.Text;
                        w8.label4.Content = textBox6.Text;
                        w8.label3.Content = textBox7.Text;
                        w8.label2.Content = textBox8.Text;
                        w8.label1.Content = textBox1.Text;
                        w8.label.Content = textBox2.Text;
                    }
                    else if (textBox4.Text == sync)
                    {
                        w8.label7.Content = textBox4.Text;
                        w8.label6.Content = textBox5.Text;
                        w8.label5.Content = textBox6.Text;
                        w8.label4.Content = textBox7.Text;
                        w8.label3.Content = textBox8.Text;
                        w8.label2.Content = textBox1.Text;
                        w8.label1.Content = textBox2.Text;
                        w8.label.Content = textBox3.Text;
                    }
                    else if (textBox5.Text == sync)
                    {
                        w8.label7.Content = textBox5.Text;
                        w8.label6.Content = textBox6.Text;
                        w8.label5.Content = textBox7.Text;
                        w8.label4.Content = textBox8.Text;
                        w8.label3.Content = textBox1.Text;
                        w8.label2.Content = textBox2.Text;
                        w8.label1.Content = textBox3.Text;
                        w8.label.Content = textBox4.Text;
                    }
                    else if (textBox6.Text == sync)
                    {
                        w8.label7.Content = textBox6.Text;
                        w8.label6.Content = textBox7.Text;
                        w8.label5.Content = textBox8.Text;
                        w8.label4.Content = textBox1.Text;
                        w8.label3.Content = textBox2.Text;
                        w8.label2.Content = textBox3.Text;
                        w8.label1.Content = textBox4.Text;
                        w8.label.Content = textBox5.Text;
                    }
                    else if (textBox7.Text == sync)
                    {
                        w8.label7.Content = textBox7.Text;
                        w8.label6.Content = textBox8.Text;
                        w8.label5.Content = textBox1.Text;
                        w8.label4.Content = textBox2.Text;
                        w8.label3.Content = textBox3.Text;
                        w8.label2.Content = textBox4.Text;
                        w8.label1.Content = textBox5.Text;
                        w8.label.Content = textBox6.Text;
                    }
                    else if (textBox8.Text == sync)
                    {
                        w8.label7.Content = textBox8.Text;
                        w8.label6.Content = textBox1.Text;
                        w8.label5.Content = textBox2.Text;
                        w8.label4.Content = textBox3.Text;
                        w8.label3.Content = textBox4.Text;
                        w8.label2.Content = textBox5.Text;
                        w8.label1.Content = textBox6.Text;
                        w8.label.Content = textBox7.Text;
                    }
                    w8.Show();
                }
                else if (comboBoxCylinders.Text == "9")
                {
                    Window9 w9 = new Window9();
                    string sync = comboBoxSync.Text;

                    if (comboBox1.Text == sync)
                    {
                        w9.label8.Content = comboBox1.Text;
                        w9.label7.Content = comboBox2.Text;
                        w9.label6.Content = comboBox3.Text;
                        w9.label5.Content = comboBox4.Text;
                        w9.label4.Content = comboBox5.Text;
                        w9.label3.Content = comboBox6.Text;
                        w9.label2.Content = comboBox7.Text;
                        w9.label1.Content = comboBox8.Text;
                        w9.label.Content = comboBox9.Text;

                    }
                    else if (comboBox2.Text == sync)
                    {
                        w9.label8.Content = comboBox2.Text;
                        w9.label7.Content = comboBox3.Text;
                        w9.label6.Content = comboBox4.Text;
                        w9.label5.Content = comboBox5.Text;
                        w9.label4.Content = comboBox6.Text;
                        w9.label3.Content = comboBox7.Text;
                        w9.label2.Content = comboBox8.Text;
                        w9.label1.Content = comboBox9.Text;
                        w9.label.Content = comboBox1.Text;
                    }
                    else if (comboBox3.Text == sync)
                    {
                        w9.label8.Content = comboBox3.Text;
                        w9.label7.Content = comboBox4.Text;
                        w9.label6.Content = comboBox5.Text;
                        w9.label5.Content = comboBox6.Text;
                        w9.label4.Content = comboBox7.Text;
                        w9.label3.Content = comboBox8.Text;
                        w9.label2.Content = comboBox9.Text;
                        w9.label1.Content = comboBox1.Text;
                        w9.label.Content = comboBox2.Text;
                    }
                    else if (comboBox4.Text == sync)
                    {
                        w9.label8.Content = comboBox4.Text;
                        w9.label7.Content = comboBox5.Text;
                        w9.label6.Content = comboBox6.Text;
                        w9.label5.Content = comboBox7.Text;
                        w9.label4.Content = comboBox8.Text;
                        w9.label3.Content = comboBox9.Text;
                        w9.label2.Content = comboBox1.Text;
                        w9.label1.Content = comboBox2.Text;
                        w9.label.Content = comboBox3.Text;
                    }
                    else if (comboBox5.Text == sync)
                    {
                        w9.label8.Content = comboBox5.Text;
                        w9.label7.Content = comboBox6.Text;
                        w9.label6.Content = comboBox7.Text;
                        w9.label5.Content = comboBox8.Text;
                        w9.label4.Content = comboBox9.Text;
                        w9.label3.Content = comboBox1.Text;
                        w9.label2.Content = comboBox2.Text;
                        w9.label1.Content = comboBox3.Text;
                        w9.label.Content = comboBox4.Text;
                    }
                    else if (comboBox6.Text == sync)
                    {
                        w9.label8.Content = comboBox6.Text;
                        w9.label7.Content = comboBox7.Text;
                        w9.label6.Content = comboBox8.Text;
                        w9.label5.Content = comboBox9.Text;
                        w9.label4.Content = comboBox1.Text;
                        w9.label3.Content = comboBox2.Text;
                        w9.label2.Content = comboBox3.Text;
                        w9.label1.Content = comboBox4.Text;
                        w9.label.Content = comboBox5.Text;
                    }
                    else if (comboBox7.Text == sync)
                    {
                        w9.label8.Content = comboBox7.Text;
                        w9.label7.Content = comboBox8.Text;
                        w9.label6.Content = comboBox9.Text;
                        w9.label5.Content = comboBox1.Text;
                        w9.label4.Content = comboBox2.Text;
                        w9.label3.Content = comboBox3.Text;
                        w9.label2.Content = comboBox4.Text;
                        w9.label1.Content = comboBox5.Text;
                        w9.label.Content = comboBox6.Text;
                    }
                    else if (comboBox8.Text == sync)
                    {
                        w9.label8.Content = comboBox8.Text;
                        w9.label7.Content = comboBox9.Text;
                        w9.label6.Content = comboBox1.Text;
                        w9.label5.Content = comboBox2.Text;
                        w9.label4.Content = comboBox3.Text;
                        w9.label3.Content = comboBox4.Text;
                        w9.label2.Content = comboBox5.Text;
                        w9.label1.Content = comboBox6.Text;
                        w9.label.Content = comboBox7.Text;
                    }
                    else if (comboBox9.Text == sync)
                    {
                        w9.label8.Content = comboBox9.Text;
                        w9.label7.Content = comboBox1.Text;
                        w9.label6.Content = comboBox2.Text;
                        w9.label5.Content = comboBox3.Text;
                        w9.label4.Content = comboBox4.Text;
                        w9.label3.Content = comboBox5.Text;
                        w9.label2.Content = comboBox6.Text;
                        w9.label1.Content = comboBox7.Text;
                        w9.label.Content = comboBox8.Text;
                    }
                    w9.Show();
                }
                else if (comboBoxCylinders.Text == "10")
                {
                    Window10 w10 = new Window10();
                    string sync = comboBoxSync.Text;

                    if (comboBox1.Text == sync)
                    {
                        w10.label9.Content = comboBox1.Text;
                        w10.label8.Content = comboBox2.Text;
                        w10.label7.Content = comboBox3.Text;
                        w10.label6.Content = comboBox4.Text;
                        w10.label5.Content = comboBox5.Text;
                        w10.label4.Content = comboBox6.Text;
                        w10.label3.Content = comboBox7.Text;
                        w10.label2.Content = comboBox8.Text;
                        w10.label1.Content = comboBox9.Text;
                        w10.label.Content = comboBox10.Text;

                    }
                    else if (comboBox2.Text == sync)
                    {
                        w10.label9.Content = comboBox2.Text;
                        w10.label8.Content = comboBox3.Text;
                        w10.label7.Content = comboBox4.Text;
                        w10.label6.Content = comboBox5.Text;
                        w10.label5.Content = comboBox6.Text;
                        w10.label4.Content = comboBox7.Text;
                        w10.label3.Content = comboBox8.Text;
                        w10.label2.Content = comboBox9.Text;
                        w10.label1.Content = comboBox10.Text;
                        w10.label.Content = comboBox1.Text;
                    }
                    else if (comboBox3.Text == sync)
                    {
                        w10.label9.Content = comboBox3.Text;
                        w10.label8.Content = comboBox4.Text;
                        w10.label7.Content = comboBox5.Text;
                        w10.label6.Content = comboBox6.Text;
                        w10.label5.Content = comboBox7.Text;
                        w10.label4.Content = comboBox8.Text;
                        w10.label3.Content = comboBox9.Text;
                        w10.label2.Content = comboBox10.Text;
                        w10.label1.Content = comboBox1.Text;
                        w10.label.Content = comboBox2.Text;
                    }
                    else if (comboBox4.Text == sync)
                    {
                        w10.label9.Content = comboBox4.Text;
                        w10.label8.Content = comboBox5.Text;
                        w10.label7.Content = comboBox6.Text;
                        w10.label6.Content = comboBox7.Text;
                        w10.label5.Content = comboBox8.Text;
                        w10.label4.Content = comboBox9.Text;
                        w10.label3.Content = comboBox10.Text;
                        w10.label2.Content = comboBox1.Text;
                        w10.label1.Content = comboBox2.Text;
                        w10.label.Content = comboBox3.Text;
                    }
                    else if (comboBox5.Text == sync)
                    {
                        w10.label9.Content = comboBox5.Text;
                        w10.label8.Content = comboBox6.Text;
                        w10.label7.Content = comboBox7.Text;
                        w10.label6.Content = comboBox8.Text;
                        w10.label5.Content = comboBox9.Text;
                        w10.label4.Content = comboBox10.Text;
                        w10.label3.Content = comboBox1.Text;
                        w10.label2.Content = comboBox2.Text;
                        w10.label1.Content = comboBox3.Text;
                        w10.label.Content = comboBox4.Text;
                    }
                    else if (comboBox6.Text == sync)
                    {
                        w10.label9.Content = comboBox6.Text;
                        w10.label8.Content = comboBox7.Text;
                        w10.label7.Content = comboBox8.Text;
                        w10.label6.Content = comboBox9.Text;
                        w10.label5.Content = comboBox10.Text;
                        w10.label4.Content = comboBox1.Text;
                        w10.label3.Content = comboBox2.Text;
                        w10.label2.Content = comboBox3.Text;
                        w10.label1.Content = comboBox4.Text;
                        w10.label.Content = comboBox5.Text;
                    }
                    else if (comboBox7.Text == sync)
                    {
                        w10.label9.Content = comboBox7.Text;
                        w10.label8.Content = comboBox8.Text;
                        w10.label7.Content = comboBox9.Text;
                        w10.label6.Content = comboBox10.Text;
                        w10.label5.Content = comboBox1.Text;
                        w10.label4.Content = comboBox2.Text;
                        w10.label3.Content = comboBox3.Text;
                        w10.label2.Content = comboBox4.Text;
                        w10.label1.Content = comboBox5.Text;
                        w10.label.Content = comboBox6.Text;
                    }
                    else if (comboBox8.Text == sync)
                    {
                        w10.label9.Content = comboBox8.Text;
                        w10.label8.Content = comboBox9.Text;
                        w10.label7.Content = comboBox10.Text;
                        w10.label6.Content = comboBox1.Text;
                        w10.label5.Content = comboBox2.Text;
                        w10.label4.Content = comboBox3.Text;
                        w10.label3.Content = comboBox4.Text;
                        w10.label2.Content = comboBox5.Text;
                        w10.label1.Content = comboBox6.Text;
                        w10.label.Content = comboBox7.Text;
                    }
                    else if (comboBox9.Text == sync)
                    {
                        w10.label9.Content = comboBox9.Text;
                        w10.label8.Content = comboBox10.Text;
                        w10.label7.Content = comboBox1.Text;
                        w10.label6.Content = comboBox2.Text;
                        w10.label5.Content = comboBox3.Text;
                        w10.label4.Content = comboBox4.Text;
                        w10.label3.Content = comboBox5.Text;
                        w10.label2.Content = comboBox6.Text;
                        w10.label1.Content = comboBox7.Text;
                        w10.label.Content = comboBox8.Text;
                    }
                    else if (comboBox10.Text == sync)
                    {
                        w10.label9.Content = comboBox10.Text;
                        w10.label8.Content = comboBox1.Text;
                        w10.label7.Content = comboBox2.Text;
                        w10.label6.Content = comboBox3.Text;
                        w10.label5.Content = comboBox4.Text;
                        w10.label4.Content = comboBox5.Text;
                        w10.label3.Content = comboBox6.Text;
                        w10.label2.Content = comboBox7.Text;
                        w10.label1.Content = comboBox8.Text;
                        w10.label.Content = comboBox9.Text;
                    }
                    w10.Show();
                }
                else if (comboBoxCylinders.Text == "11")
                {
                    Window11 w11 = new Window11();
                    string sync = comboBoxSync.Text;

                    if (comboBox1.Text == sync)
                    {
                        w11.label10.Content = comboBox1.Text;
                        w11.label9.Content = comboBox2.Text;
                        w11.label8.Content = comboBox3.Text;
                        w11.label7.Content = comboBox4.Text;
                        w11.label6.Content = comboBox5.Text;
                        w11.label5.Content = comboBox6.Text;
                        w11.label4.Content = comboBox7.Text;
                        w11.label3.Content = comboBox8.Text;
                        w11.label2.Content = comboBox9.Text;
                        w11.label1.Content = comboBox10.Text;
                        w11.label.Content = comboBox11.Text;
                    }
                    else if (comboBox2.Text == sync)
                    {
                        w11.label10.Content = comboBox2.Text;
                        w11.label9.Content = comboBox3.Text;
                        w11.label8.Content = comboBox4.Text;
                        w11.label7.Content = comboBox5.Text;
                        w11.label6.Content = comboBox6.Text;
                        w11.label5.Content = comboBox7.Text;
                        w11.label4.Content = comboBox8.Text;
                        w11.label3.Content = comboBox9.Text;
                        w11.label2.Content = comboBox10.Text;
                        w11.label1.Content = comboBox11.Text;
                        w11.label.Content = comboBox1.Text;
                    }
                    else if (comboBox3.Text == sync)
                    {
                        w11.label10.Content = comboBox3.Text;
                        w11.label9.Content = comboBox4.Text;
                        w11.label8.Content = comboBox5.Text;
                        w11.label7.Content = comboBox6.Text;
                        w11.label6.Content = comboBox7.Text;
                        w11.label5.Content = comboBox8.Text;
                        w11.label4.Content = comboBox9.Text;
                        w11.label3.Content = comboBox10.Text;
                        w11.label2.Content = comboBox11.Text;
                        w11.label1.Content = comboBox1.Text;
                        w11.label.Content = comboBox2.Text;
                    }
                    else if (comboBox4.Text == sync)
                    {
                        w11.label10.Content = comboBox4.Text;
                        w11.label9.Content = comboBox5.Text;
                        w11.label8.Content = comboBox6.Text;
                        w11.label7.Content = comboBox7.Text;
                        w11.label6.Content = comboBox8.Text;
                        w11.label5.Content = comboBox9.Text;
                        w11.label4.Content = comboBox10.Text;
                        w11.label3.Content = comboBox11.Text;
                        w11.label2.Content = comboBox1.Text;
                        w11.label1.Content = comboBox2.Text;
                        w11.label.Content = comboBox3.Text;
                    }
                    else if (comboBox5.Text == sync)
                    {
                        w11.label10.Content = comboBox5.Text;
                        w11.label9.Content = comboBox6.Text;
                        w11.label8.Content = comboBox7.Text;
                        w11.label7.Content = comboBox8.Text;
                        w11.label6.Content = comboBox9.Text;
                        w11.label5.Content = comboBox10.Text;
                        w11.label4.Content = comboBox11.Text;
                        w11.label3.Content = comboBox1.Text;
                        w11.label2.Content = comboBox2.Text;
                        w11.label1.Content = comboBox3.Text;
                        w11.label.Content = comboBox4.Text;
                    }
                    else if (comboBox6.Text == sync)
                    {
                        w11.label10.Content = comboBox6.Text;
                        w11.label9.Content = comboBox7.Text;
                        w11.label8.Content = comboBox8.Text;
                        w11.label7.Content = comboBox9.Text;
                        w11.label6.Content = comboBox10.Text;
                        w11.label5.Content = comboBox11.Text;
                        w11.label4.Content = comboBox1.Text;
                        w11.label3.Content = comboBox2.Text;
                        w11.label2.Content = comboBox3.Text;
                        w11.label1.Content = comboBox4.Text;
                        w11.label.Content = comboBox5.Text;
                    }
                    else if (comboBox7.Text == sync)
                    {
                        w11.label10.Content = comboBox7.Text;
                        w11.label9.Content = comboBox8.Text;
                        w11.label8.Content = comboBox9.Text;
                        w11.label7.Content = comboBox10.Text;
                        w11.label6.Content = comboBox11.Text;
                        w11.label5.Content = comboBox1.Text;
                        w11.label4.Content = comboBox2.Text;
                        w11.label3.Content = comboBox3.Text;
                        w11.label2.Content = comboBox4.Text;
                        w11.label1.Content = comboBox5.Text;
                        w11.label.Content = comboBox6.Text;
                    }
                    else if (comboBox8.Text == sync)
                    {
                        w11.label10.Content = comboBox8.Text;
                        w11.label9.Content = comboBox9.Text;
                        w11.label8.Content = comboBox10.Text;
                        w11.label7.Content = comboBox11.Text;
                        w11.label6.Content = comboBox1.Text;
                        w11.label5.Content = comboBox2.Text;
                        w11.label4.Content = comboBox3.Text;
                        w11.label3.Content = comboBox4.Text;
                        w11.label2.Content = comboBox5.Text;
                        w11.label1.Content = comboBox6.Text;
                        w11.label.Content = comboBox7.Text;
                    }
                    else if (comboBox9.Text == sync)
                    {
                        w11.label10.Content = comboBox9.Text;
                        w11.label9.Content = comboBox10.Text;
                        w11.label8.Content = comboBox11.Text;
                        w11.label7.Content = comboBox1.Text;
                        w11.label6.Content = comboBox2.Text;
                        w11.label5.Content = comboBox3.Text;
                        w11.label4.Content = comboBox4.Text;
                        w11.label3.Content = comboBox5.Text;
                        w11.label2.Content = comboBox6.Text;
                        w11.label1.Content = comboBox7.Text;
                        w11.label.Content = comboBox8.Text;
                    }
                    else if (comboBox10.Text == sync)
                    {
                        w11.label10.Content = comboBox10.Text;
                        w11.label9.Content = comboBox11.Text;
                        w11.label8.Content = comboBox1.Text;
                        w11.label7.Content = comboBox2.Text;
                        w11.label6.Content = comboBox3.Text;
                        w11.label5.Content = comboBox4.Text;
                        w11.label4.Content = comboBox5.Text;
                        w11.label3.Content = comboBox6.Text;
                        w11.label2.Content = comboBox7.Text;
                        w11.label1.Content = comboBox8.Text;
                        w11.label.Content = comboBox9.Text;
                    }
                    else if (comboBox11.Text == sync)
                    {
                        w11.label10.Content = comboBox11.Text;
                        w11.label9.Content = comboBox1.Text;
                        w11.label8.Content = comboBox2.Text;
                        w11.label7.Content = comboBox3.Text;
                        w11.label6.Content = comboBox4.Text;
                        w11.label5.Content = comboBox5.Text;
                        w11.label4.Content = comboBox6.Text;
                        w11.label3.Content = comboBox7.Text;
                        w11.label2.Content = comboBox8.Text;
                        w11.label1.Content = comboBox9.Text;
                        w11.label.Content = comboBox10.Text;
                    }
                    w11.Show();
                }
                else if (comboBoxCylinders.Text == "12")
                {
                    Window12 w12 = new Window12();
                    string sync = comboBoxSync.Text;

                    if (comboBox1.Text == sync)
                    {
                        w12.label11.Content = comboBox1.Text;
                        w12.label10.Content = comboBox2.Text;
                        w12.label9.Content = comboBox3.Text;
                        w12.label8.Content = comboBox4.Text;
                        w12.label7.Content = comboBox5.Text;
                        w12.label6.Content = comboBox6.Text;
                        w12.label5.Content = comboBox7.Text;
                        w12.label4.Content = comboBox8.Text;
                        w12.label3.Content = comboBox9.Text;
                        w12.label2.Content = comboBox10.Text;
                        w12.label1.Content = comboBox11.Text;
                        w12.label.Content = comboBox12.Text;
                    }
                    else if (comboBox2.Text == sync)
                    {
                        w12.label11.Content = comboBox2.Text;
                        w12.label10.Content = comboBox3.Text;
                        w12.label9.Content = comboBox4.Text;
                        w12.label8.Content = comboBox5.Text;
                        w12.label7.Content = comboBox6.Text;
                        w12.label6.Content = comboBox7.Text;
                        w12.label5.Content = comboBox8.Text;
                        w12.label4.Content = comboBox9.Text;
                        w12.label3.Content = comboBox10.Text;
                        w12.label2.Content = comboBox11.Text;
                        w12.label1.Content = comboBox12.Text;
                        w12.label.Content = comboBox1.Text;
                    }
                    else if (comboBox3.Text == sync)
                    {
                        w12.label11.Content = comboBox3.Text;
                        w12.label10.Content = comboBox4.Text;
                        w12.label9.Content = comboBox5.Text;
                        w12.label8.Content = comboBox6.Text;
                        w12.label7.Content = comboBox7.Text;
                        w12.label6.Content = comboBox8.Text;
                        w12.label5.Content = comboBox9.Text;
                        w12.label4.Content = comboBox10.Text;
                        w12.label3.Content = comboBox11.Text;
                        w12.label2.Content = comboBox12.Text;
                        w12.label1.Content = comboBox1.Text;
                        w12.label.Content = comboBox2.Text;
                    }
                    else if (comboBox4.Text == sync)
                    {
                        w12.label11.Content = comboBox4.Text;
                        w12.label10.Content = comboBox5.Text;
                        w12.label9.Content = comboBox6.Text;
                        w12.label8.Content = comboBox7.Text;
                        w12.label7.Content = comboBox8.Text;
                        w12.label6.Content = comboBox9.Text;
                        w12.label5.Content = comboBox10.Text;
                        w12.label4.Content = comboBox11.Text;
                        w12.label3.Content = comboBox12.Text;
                        w12.label2.Content = comboBox1.Text;
                        w12.label1.Content = comboBox2.Text;
                        w12.label.Content = comboBox3.Text;
                    }
                    else if (comboBox5.Text == sync)
                    {
                        w12.label11.Content = comboBox5.Text;
                        w12.label10.Content = comboBox6.Text;
                        w12.label9.Content = comboBox7.Text;
                        w12.label8.Content = comboBox8.Text;
                        w12.label7.Content = comboBox9.Text;
                        w12.label6.Content = comboBox10.Text;
                        w12.label5.Content = comboBox11.Text;
                        w12.label4.Content = comboBox12.Text;
                        w12.label3.Content = comboBox1.Text;
                        w12.label2.Content = comboBox2.Text;
                        w12.label1.Content = comboBox3.Text;
                        w12.label.Content = comboBox4.Text;
                    }
                    else if (comboBox6.Text == sync)
                    {
                        w12.label11.Content = comboBox6.Text;
                        w12.label10.Content = comboBox7.Text;
                        w12.label9.Content = comboBox8.Text;
                        w12.label8.Content = comboBox9.Text;
                        w12.label7.Content = comboBox10.Text;
                        w12.label6.Content = comboBox11.Text;
                        w12.label5.Content = comboBox12.Text;
                        w12.label4.Content = comboBox1.Text;
                        w12.label3.Content = comboBox2.Text;
                        w12.label2.Content = comboBox3.Text;
                        w12.label1.Content = comboBox4.Text;
                        w12.label.Content = comboBox5.Text;
                    }
                    else if (comboBox7.Text == sync)
                    {
                        w12.label11.Content = comboBox7.Text;
                        w12.label10.Content = comboBox8.Text;
                        w12.label9.Content = comboBox9.Text;
                        w12.label8.Content = comboBox10.Text;
                        w12.label7.Content = comboBox11.Text;
                        w12.label6.Content = comboBox12.Text;
                        w12.label5.Content = comboBox1.Text;
                        w12.label4.Content = comboBox2.Text;
                        w12.label3.Content = comboBox3.Text;
                        w12.label2.Content = comboBox4.Text;
                        w12.label1.Content = comboBox5.Text;
                        w12.label.Content = comboBox6.Text;
                    }
                    else if (comboBox8.Text == sync)
                    {
                        w12.label11.Content = comboBox8.Text;
                        w12.label10.Content = comboBox9.Text;
                        w12.label9.Content = comboBox10.Text;
                        w12.label8.Content = comboBox11.Text;
                        w12.label7.Content = comboBox12.Text;
                        w12.label6.Content = comboBox1.Text;
                        w12.label5.Content = comboBox2.Text;
                        w12.label4.Content = comboBox3.Text;
                        w12.label3.Content = comboBox4.Text;
                        w12.label2.Content = comboBox5.Text;
                        w12.label1.Content = comboBox6.Text;
                        w12.label.Content = comboBox7.Text;
                    }
                    else if (comboBox9.Text == sync)
                    {
                        w12.label11.Content = comboBox9.Text;
                        w12.label10.Content = comboBox10.Text;
                        w12.label9.Content = comboBox11.Text;
                        w12.label8.Content = comboBox12.Text;
                        w12.label7.Content = comboBox1.Text;
                        w12.label6.Content = comboBox2.Text;
                        w12.label5.Content = comboBox3.Text;
                        w12.label4.Content = comboBox4.Text;
                        w12.label3.Content = comboBox5.Text;
                        w12.label2.Content = comboBox6.Text;
                        w12.label1.Content = comboBox7.Text;
                        w12.label.Content = comboBox8.Text;
                    }
                    else if (comboBox10.Text == sync)
                    {
                        w12.label11.Content = comboBox10.Text;
                        w12.label10.Content = comboBox11.Text;
                        w12.label9.Content = comboBox12.Text;
                        w12.label8.Content = comboBox1.Text;
                        w12.label7.Content = comboBox2.Text;
                        w12.label6.Content = comboBox3.Text;
                        w12.label5.Content = comboBox4.Text;
                        w12.label4.Content = comboBox5.Text;
                        w12.label3.Content = comboBox6.Text;
                        w12.label2.Content = comboBox7.Text;
                        w12.label1.Content = comboBox8.Text;
                        w12.label.Content = comboBox9.Text;
                    }
                    else if (comboBox11.Text == sync)
                    {
                        w12.label11.Content = comboBox11.Text;
                        w12.label10.Content = comboBox12.Text;
                        w12.label9.Content = comboBox1.Text;
                        w12.label8.Content = comboBox2.Text;
                        w12.label7.Content = comboBox3.Text;
                        w12.label6.Content = comboBox4.Text;
                        w12.label5.Content = comboBox5.Text;
                        w12.label4.Content = comboBox6.Text;
                        w12.label3.Content = comboBox7.Text;
                        w12.label2.Content = comboBox8.Text;
                        w12.label1.Content = comboBox9.Text;
                        w12.label.Content = comboBox10.Text;
                    }
                    else if (comboBox12.Text == sync)
                    {
                        w12.label11.Content = comboBox12.Text;
                        w12.label10.Content = comboBox1.Text;
                        w12.label9.Content = comboBox2.Text;
                        w12.label8.Content = comboBox3.Text;
                        w12.label7.Content = comboBox4.Text;
                        w12.label6.Content = comboBox5.Text;
                        w12.label5.Content = comboBox6.Text;
                        w12.label4.Content = comboBox7.Text;
                        w12.label3.Content = comboBox8.Text;
                        w12.label2.Content = comboBox9.Text;
                        w12.label1.Content = comboBox10.Text;
                        w12.label.Content = comboBox11.Text;
                    }
                    w12.Show();
                }
            }
        }

        private void comboBoxCylinders_DropDownClosed(object sender, EventArgs e)
        {
            if (comboBoxCylinders.SelectedItem == null)
            {

            }
            else
            {
                var cb = sender as ComboBox;
                int cbNum = Convert.ToInt32(cb.SelectionBoxItem);

                if (cbNum == 1)
                {
                    //Clear and reset the form
                    comboBox1.Visibility = Visibility.Hidden; comboBox2.Visibility = Visibility.Hidden; comboBox3.Visibility = Visibility.Hidden; comboBox4.Visibility = Visibility.Hidden; comboBox5.Visibility = Visibility.Hidden; comboBox6.Visibility = Visibility.Hidden; comboBox7.Visibility = Visibility.Hidden; comboBox8.Visibility = Visibility.Hidden; comboBox9.Visibility = Visibility.Hidden; comboBox10.Visibility = Visibility.Hidden; comboBox11.Visibility = Visibility.Hidden; comboBox12.Visibility = Visibility.Hidden;
                    textBox1.Visibility = Visibility.Hidden; textBox2.Visibility = Visibility.Hidden; textBox3.Visibility = Visibility.Hidden; textBox4.Visibility = Visibility.Hidden; textBox5.Visibility = Visibility.Hidden; textBox6.Visibility = Visibility.Hidden; textBox7.Visibility = Visibility.Hidden; textBox8.Visibility = Visibility.Hidden;
                    textBox1.Text = ""; textBox2.Text = ""; textBox3.Text = ""; textBox4.Text = ""; textBox5.Text = ""; textBox6.Text = ""; textBox7.Text = ""; textBox8.Text = "";
                    comboBoxSync.Items.Clear();
                    Application.Current.MainWindow.Width = 465;

                    textBox1.Visibility = Visibility.Visible;
                    textBox1.Text = "1";

                    comboBoxSync.Items.Add(1);
                    comboBoxSync.SelectedIndex = 0;
                }
                else if (cbNum == 2)
                {
                    //Clear and reset the form
                    comboBox1.Visibility = Visibility.Hidden; comboBox2.Visibility = Visibility.Hidden; comboBox3.Visibility = Visibility.Hidden; comboBox4.Visibility = Visibility.Hidden; comboBox5.Visibility = Visibility.Hidden; comboBox6.Visibility = Visibility.Hidden; comboBox7.Visibility = Visibility.Hidden; comboBox8.Visibility = Visibility.Hidden; comboBox9.Visibility = Visibility.Hidden; comboBox10.Visibility = Visibility.Hidden; comboBox11.Visibility = Visibility.Hidden; comboBox12.Visibility = Visibility.Hidden;
                    textBox1.Visibility = Visibility.Hidden; textBox2.Visibility = Visibility.Hidden; textBox3.Visibility = Visibility.Hidden; textBox4.Visibility = Visibility.Hidden; textBox5.Visibility = Visibility.Hidden; textBox6.Visibility = Visibility.Hidden; textBox7.Visibility = Visibility.Hidden; textBox8.Visibility = Visibility.Hidden;
                    textBox1.Text = ""; textBox2.Text = ""; textBox3.Text = ""; textBox4.Text = ""; textBox5.Text = ""; textBox6.Text = ""; textBox7.Text = ""; textBox8.Text = "";
                    comboBoxSync.Items.Clear();
                    Application.Current.MainWindow.Width = 465;

                    textBox1.Visibility = Visibility.Visible;
                    textBox2.Visibility = Visibility.Visible;

                    comboBoxSync.Items.Add(1);
                    comboBoxSync.Items.Add(2);

                    Keyboard.Focus(textBox1);
                }
                else if (cbNum == 3)
                {
                    //Clear and reset the form
                    comboBox1.Visibility = Visibility.Hidden; comboBox2.Visibility = Visibility.Hidden; comboBox3.Visibility = Visibility.Hidden; comboBox4.Visibility = Visibility.Hidden; comboBox5.Visibility = Visibility.Hidden; comboBox6.Visibility = Visibility.Hidden; comboBox7.Visibility = Visibility.Hidden; comboBox8.Visibility = Visibility.Hidden; comboBox9.Visibility = Visibility.Hidden; comboBox10.Visibility = Visibility.Hidden; comboBox11.Visibility = Visibility.Hidden; comboBox12.Visibility = Visibility.Hidden;
                    textBox1.Visibility = Visibility.Hidden; textBox2.Visibility = Visibility.Hidden; textBox3.Visibility = Visibility.Hidden; textBox4.Visibility = Visibility.Hidden; textBox5.Visibility = Visibility.Hidden; textBox6.Visibility = Visibility.Hidden; textBox7.Visibility = Visibility.Hidden; textBox8.Visibility = Visibility.Hidden;
                    textBox1.Text = ""; textBox2.Text = ""; textBox3.Text = ""; textBox4.Text = ""; textBox5.Text = ""; textBox6.Text = ""; textBox7.Text = ""; textBox8.Text = "";
                    comboBoxSync.Items.Clear();
                    Application.Current.MainWindow.Width = 465;

                    textBox1.Visibility = Visibility.Visible;
                    textBox2.Visibility = Visibility.Visible;
                    textBox3.Visibility = Visibility.Visible;

                    comboBoxSync.Items.Add(1);
                    comboBoxSync.Items.Add(2);
                    comboBoxSync.Items.Add(3);

                    Keyboard.Focus(textBox1);
                }
                else if (cbNum == 4)
                {
                    //Clear and reset the form
                    comboBox1.Visibility = Visibility.Hidden; comboBox2.Visibility = Visibility.Hidden; comboBox3.Visibility = Visibility.Hidden; comboBox4.Visibility = Visibility.Hidden; comboBox5.Visibility = Visibility.Hidden; comboBox6.Visibility = Visibility.Hidden; comboBox7.Visibility = Visibility.Hidden; comboBox8.Visibility = Visibility.Hidden; comboBox9.Visibility = Visibility.Hidden; comboBox10.Visibility = Visibility.Hidden; comboBox11.Visibility = Visibility.Hidden; comboBox12.Visibility = Visibility.Hidden;
                    textBox1.Visibility = Visibility.Hidden; textBox2.Visibility = Visibility.Hidden; textBox3.Visibility = Visibility.Hidden; textBox4.Visibility = Visibility.Hidden; textBox5.Visibility = Visibility.Hidden; textBox6.Visibility = Visibility.Hidden; textBox7.Visibility = Visibility.Hidden; textBox8.Visibility = Visibility.Hidden;
                    textBox1.Text = ""; textBox2.Text = ""; textBox3.Text = ""; textBox4.Text = ""; textBox5.Text = ""; textBox6.Text = ""; textBox7.Text = ""; textBox8.Text = "";
                    comboBoxSync.Items.Clear();
                    Application.Current.MainWindow.Width = 465;

                    textBox1.Visibility = Visibility.Visible;
                    textBox2.Visibility = Visibility.Visible;
                    textBox3.Visibility = Visibility.Visible;
                    textBox4.Visibility = Visibility.Visible;

                    comboBoxSync.Items.Add(1);
                    comboBoxSync.Items.Add(2);
                    comboBoxSync.Items.Add(3);
                    comboBoxSync.Items.Add(4);

                    Keyboard.Focus(textBox1);
                }
                else if (cbNum == 5)
                {
                    //Clear and reset the form
                    comboBox1.Visibility = Visibility.Hidden; comboBox2.Visibility = Visibility.Hidden; comboBox3.Visibility = Visibility.Hidden; comboBox4.Visibility = Visibility.Hidden; comboBox5.Visibility = Visibility.Hidden; comboBox6.Visibility = Visibility.Hidden; comboBox7.Visibility = Visibility.Hidden; comboBox8.Visibility = Visibility.Hidden; comboBox9.Visibility = Visibility.Hidden; comboBox10.Visibility = Visibility.Hidden; comboBox11.Visibility = Visibility.Hidden; comboBox12.Visibility = Visibility.Hidden;
                    textBox1.Visibility = Visibility.Hidden; textBox2.Visibility = Visibility.Hidden; textBox3.Visibility = Visibility.Hidden; textBox4.Visibility = Visibility.Hidden; textBox5.Visibility = Visibility.Hidden; textBox6.Visibility = Visibility.Hidden; textBox7.Visibility = Visibility.Hidden; textBox8.Visibility = Visibility.Hidden;
                    textBox1.Text = ""; textBox2.Text = ""; textBox3.Text = ""; textBox4.Text = ""; textBox5.Text = ""; textBox6.Text = ""; textBox7.Text = ""; textBox8.Text = "";
                    comboBoxSync.Items.Clear();
                    Application.Current.MainWindow.Width = 465;

                    textBox1.Visibility = Visibility.Visible;
                    textBox2.Visibility = Visibility.Visible;
                    textBox3.Visibility = Visibility.Visible;
                    textBox4.Visibility = Visibility.Visible;
                    textBox5.Visibility = Visibility.Visible;

                    comboBoxSync.Items.Add(1);
                    comboBoxSync.Items.Add(2);
                    comboBoxSync.Items.Add(3);
                    comboBoxSync.Items.Add(4);
                    comboBoxSync.Items.Add(5);

                    Keyboard.Focus(textBox1);
                }
                else if (cbNum == 6)
                {
                    //Clear and reset the form
                    comboBox1.Visibility = Visibility.Hidden; comboBox2.Visibility = Visibility.Hidden; comboBox3.Visibility = Visibility.Hidden; comboBox4.Visibility = Visibility.Hidden; comboBox5.Visibility = Visibility.Hidden; comboBox6.Visibility = Visibility.Hidden; comboBox7.Visibility = Visibility.Hidden; comboBox8.Visibility = Visibility.Hidden; comboBox9.Visibility = Visibility.Hidden; comboBox10.Visibility = Visibility.Hidden; comboBox11.Visibility = Visibility.Hidden; comboBox12.Visibility = Visibility.Hidden;
                    textBox1.Visibility = Visibility.Hidden; textBox2.Visibility = Visibility.Hidden; textBox3.Visibility = Visibility.Hidden; textBox4.Visibility = Visibility.Hidden; textBox5.Visibility = Visibility.Hidden; textBox6.Visibility = Visibility.Hidden; textBox7.Visibility = Visibility.Hidden; textBox8.Visibility = Visibility.Hidden;
                    textBox1.Text = ""; textBox2.Text = ""; textBox3.Text = ""; textBox4.Text = ""; textBox5.Text = ""; textBox6.Text = ""; textBox7.Text = ""; textBox8.Text = "";
                    comboBoxSync.Items.Clear();
                    Application.Current.MainWindow.Width = 465;

                    textBox1.Visibility = Visibility.Visible;
                    textBox2.Visibility = Visibility.Visible;
                    textBox3.Visibility = Visibility.Visible;
                    textBox4.Visibility = Visibility.Visible;
                    textBox5.Visibility = Visibility.Visible;
                    textBox6.Visibility = Visibility.Visible;

                    comboBoxSync.Items.Add(1);
                    comboBoxSync.Items.Add(2);
                    comboBoxSync.Items.Add(3);
                    comboBoxSync.Items.Add(4);
                    comboBoxSync.Items.Add(5);
                    comboBoxSync.Items.Add(6);

                    Keyboard.Focus(textBox1);
                }
                else if (cbNum == 7)
                {
                    //Clear and reset the form
                    comboBox1.Visibility = Visibility.Hidden; comboBox2.Visibility = Visibility.Hidden; comboBox3.Visibility = Visibility.Hidden; comboBox4.Visibility = Visibility.Hidden; comboBox5.Visibility = Visibility.Hidden; comboBox6.Visibility = Visibility.Hidden; comboBox7.Visibility = Visibility.Hidden; comboBox8.Visibility = Visibility.Hidden; comboBox9.Visibility = Visibility.Hidden; comboBox10.Visibility = Visibility.Hidden; comboBox11.Visibility = Visibility.Hidden; comboBox12.Visibility = Visibility.Hidden;
                    textBox1.Visibility = Visibility.Hidden; textBox2.Visibility = Visibility.Hidden; textBox3.Visibility = Visibility.Hidden; textBox4.Visibility = Visibility.Hidden; textBox5.Visibility = Visibility.Hidden; textBox6.Visibility = Visibility.Hidden; textBox7.Visibility = Visibility.Hidden; textBox8.Visibility = Visibility.Hidden;
                    textBox1.Text = ""; textBox2.Text = ""; textBox3.Text = ""; textBox4.Text = ""; textBox5.Text = ""; textBox6.Text = ""; textBox7.Text = ""; textBox8.Text = "";
                    comboBoxSync.Items.Clear();
                    Application.Current.MainWindow.Width = 465;

                    textBox1.Visibility = Visibility.Visible;
                    textBox2.Visibility = Visibility.Visible;
                    textBox3.Visibility = Visibility.Visible;
                    textBox4.Visibility = Visibility.Visible;
                    textBox5.Visibility = Visibility.Visible;
                    textBox6.Visibility = Visibility.Visible;
                    textBox7.Visibility = Visibility.Visible;

                    comboBoxSync.Items.Add(1);
                    comboBoxSync.Items.Add(2);
                    comboBoxSync.Items.Add(3);
                    comboBoxSync.Items.Add(4);
                    comboBoxSync.Items.Add(5);
                    comboBoxSync.Items.Add(6);
                    comboBoxSync.Items.Add(7);

                    Keyboard.Focus(textBox1);
                }
                else if (cbNum == 8)
                {
                    //Clear and reset the form
                    comboBox1.Visibility = Visibility.Hidden; comboBox2.Visibility = Visibility.Hidden; comboBox3.Visibility = Visibility.Hidden; comboBox4.Visibility = Visibility.Hidden; comboBox5.Visibility = Visibility.Hidden; comboBox6.Visibility = Visibility.Hidden; comboBox7.Visibility = Visibility.Hidden; comboBox8.Visibility = Visibility.Hidden; comboBox9.Visibility = Visibility.Hidden; comboBox10.Visibility = Visibility.Hidden; comboBox11.Visibility = Visibility.Hidden; comboBox12.Visibility = Visibility.Hidden;
                    textBox1.Visibility = Visibility.Hidden; textBox2.Visibility = Visibility.Hidden; textBox3.Visibility = Visibility.Hidden; textBox4.Visibility = Visibility.Hidden; textBox5.Visibility = Visibility.Hidden; textBox6.Visibility = Visibility.Hidden; textBox7.Visibility = Visibility.Hidden; textBox8.Visibility = Visibility.Hidden;
                    textBox1.Text = ""; textBox2.Text = ""; textBox3.Text = ""; textBox4.Text = ""; textBox5.Text = ""; textBox6.Text = ""; textBox7.Text = ""; textBox8.Text = "";
                    comboBoxSync.Items.Clear();
                    Application.Current.MainWindow.Width = 465;

                    textBox1.Visibility = Visibility.Visible;
                    textBox2.Visibility = Visibility.Visible;
                    textBox3.Visibility = Visibility.Visible;
                    textBox4.Visibility = Visibility.Visible;
                    textBox5.Visibility = Visibility.Visible;
                    textBox6.Visibility = Visibility.Visible;
                    textBox7.Visibility = Visibility.Visible;
                    textBox8.Visibility = Visibility.Visible;

                    comboBoxSync.Items.Add(1);
                    comboBoxSync.Items.Add(2);
                    comboBoxSync.Items.Add(3);
                    comboBoxSync.Items.Add(4);
                    comboBoxSync.Items.Add(5);
                    comboBoxSync.Items.Add(6);
                    comboBoxSync.Items.Add(7);
                    comboBoxSync.Items.Add(8);

                    Keyboard.Focus(textBox1);
                }
                else if (cbNum == 9)
                {
                    //Clear and reset the form
                    comboBox1.Visibility = Visibility.Hidden; comboBox2.Visibility = Visibility.Hidden; comboBox3.Visibility = Visibility.Hidden; comboBox4.Visibility = Visibility.Hidden; comboBox5.Visibility = Visibility.Hidden; comboBox6.Visibility = Visibility.Hidden; comboBox7.Visibility = Visibility.Hidden; comboBox8.Visibility = Visibility.Hidden; comboBox9.Visibility = Visibility.Hidden; comboBox10.Visibility = Visibility.Hidden; comboBox11.Visibility = Visibility.Hidden; comboBox12.Visibility = Visibility.Hidden;
                    textBox1.Visibility = Visibility.Hidden; textBox2.Visibility = Visibility.Hidden; textBox3.Visibility = Visibility.Hidden; textBox4.Visibility = Visibility.Hidden; textBox5.Visibility = Visibility.Hidden; textBox6.Visibility = Visibility.Hidden; textBox7.Visibility = Visibility.Hidden; textBox8.Visibility = Visibility.Hidden;
                    textBox1.Text = ""; textBox2.Text = ""; textBox3.Text = ""; textBox4.Text = ""; textBox5.Text = ""; textBox6.Text = ""; textBox7.Text = ""; textBox8.Text = "";
                    comboBoxSync.Items.Clear();
                    Application.Current.MainWindow.Width = 625;

                    comboBox1.Visibility = Visibility.Visible;
                    comboBox2.Visibility = Visibility.Visible;
                    comboBox3.Visibility = Visibility.Visible;
                    comboBox4.Visibility = Visibility.Visible;
                    comboBox5.Visibility = Visibility.Visible;
                    comboBox6.Visibility = Visibility.Visible;
                    comboBox7.Visibility = Visibility.Visible;
                    comboBox8.Visibility = Visibility.Visible;
                    comboBox9.Visibility = Visibility.Visible;

                    comboBoxSync.Items.Add(1);
                    comboBoxSync.Items.Add(2);
                    comboBoxSync.Items.Add(3);
                    comboBoxSync.Items.Add(4);
                    comboBoxSync.Items.Add(5);
                    comboBoxSync.Items.Add(6);
                    comboBoxSync.Items.Add(7);
                    comboBoxSync.Items.Add(8);
                    comboBoxSync.Items.Add(9);
                }
                else if (cbNum == 10)
                {
                    //Clear and reset the form
                    comboBox1.Visibility = Visibility.Hidden; comboBox2.Visibility = Visibility.Hidden; comboBox3.Visibility = Visibility.Hidden; comboBox4.Visibility = Visibility.Hidden; comboBox5.Visibility = Visibility.Hidden; comboBox6.Visibility = Visibility.Hidden; comboBox7.Visibility = Visibility.Hidden; comboBox8.Visibility = Visibility.Hidden; comboBox9.Visibility = Visibility.Hidden; comboBox10.Visibility = Visibility.Hidden; comboBox11.Visibility = Visibility.Hidden; comboBox12.Visibility = Visibility.Hidden;
                    textBox1.Visibility = Visibility.Hidden; textBox2.Visibility = Visibility.Hidden; textBox3.Visibility = Visibility.Hidden; textBox4.Visibility = Visibility.Hidden; textBox5.Visibility = Visibility.Hidden; textBox6.Visibility = Visibility.Hidden; textBox7.Visibility = Visibility.Hidden; textBox8.Visibility = Visibility.Hidden;
                    textBox1.Text = ""; textBox2.Text = ""; textBox3.Text = ""; textBox4.Text = ""; textBox5.Text = ""; textBox6.Text = ""; textBox7.Text = ""; textBox8.Text = "";
                    comboBoxSync.Items.Clear();
                    Application.Current.MainWindow.Width = 665;

                    comboBox1.Visibility = Visibility.Visible;
                    comboBox2.Visibility = Visibility.Visible;
                    comboBox3.Visibility = Visibility.Visible;
                    comboBox4.Visibility = Visibility.Visible;
                    comboBox5.Visibility = Visibility.Visible;
                    comboBox6.Visibility = Visibility.Visible;
                    comboBox7.Visibility = Visibility.Visible;
                    comboBox8.Visibility = Visibility.Visible;
                    comboBox9.Visibility = Visibility.Visible;
                    comboBox10.Visibility = Visibility.Visible;

                    comboBoxSync.Items.Add(1);
                    comboBoxSync.Items.Add(2);
                    comboBoxSync.Items.Add(3);
                    comboBoxSync.Items.Add(4);
                    comboBoxSync.Items.Add(5);
                    comboBoxSync.Items.Add(6);
                    comboBoxSync.Items.Add(7);
                    comboBoxSync.Items.Add(8);
                    comboBoxSync.Items.Add(9);
                    comboBoxSync.Items.Add(10);
                }
                else if (cbNum == 11)
                {
                    //Clear and reset the form
                    comboBox1.Visibility = Visibility.Hidden; comboBox2.Visibility = Visibility.Hidden; comboBox3.Visibility = Visibility.Hidden; comboBox4.Visibility = Visibility.Hidden; comboBox5.Visibility = Visibility.Hidden; comboBox6.Visibility = Visibility.Hidden; comboBox7.Visibility = Visibility.Hidden; comboBox8.Visibility = Visibility.Hidden; comboBox9.Visibility = Visibility.Hidden; comboBox10.Visibility = Visibility.Hidden; comboBox11.Visibility = Visibility.Hidden; comboBox12.Visibility = Visibility.Hidden;
                    textBox1.Visibility = Visibility.Hidden; textBox2.Visibility = Visibility.Hidden; textBox3.Visibility = Visibility.Hidden; textBox4.Visibility = Visibility.Hidden; textBox5.Visibility = Visibility.Hidden; textBox6.Visibility = Visibility.Hidden; textBox7.Visibility = Visibility.Hidden; textBox8.Visibility = Visibility.Hidden;
                    textBox1.Text = ""; textBox2.Text = ""; textBox3.Text = ""; textBox4.Text = ""; textBox5.Text = ""; textBox6.Text = ""; textBox7.Text = ""; textBox8.Text = "";
                    comboBoxSync.Items.Clear();
                    Application.Current.MainWindow.Width = 705;

                    comboBox1.Visibility = Visibility.Visible;
                    comboBox2.Visibility = Visibility.Visible;
                    comboBox3.Visibility = Visibility.Visible;
                    comboBox4.Visibility = Visibility.Visible;
                    comboBox5.Visibility = Visibility.Visible;
                    comboBox6.Visibility = Visibility.Visible;
                    comboBox7.Visibility = Visibility.Visible;
                    comboBox8.Visibility = Visibility.Visible;
                    comboBox9.Visibility = Visibility.Visible;
                    comboBox10.Visibility = Visibility.Visible;
                    comboBox11.Visibility = Visibility.Visible;

                    comboBoxSync.Items.Add(1);
                    comboBoxSync.Items.Add(2);
                    comboBoxSync.Items.Add(3);
                    comboBoxSync.Items.Add(4);
                    comboBoxSync.Items.Add(5);
                    comboBoxSync.Items.Add(6);
                    comboBoxSync.Items.Add(7);
                    comboBoxSync.Items.Add(8);
                    comboBoxSync.Items.Add(9);
                    comboBoxSync.Items.Add(10);
                    comboBoxSync.Items.Add(11);
                }
                else if (cbNum == 12)
                {
                    //Clear and reset the form
                    comboBox1.Visibility = Visibility.Hidden; comboBox2.Visibility = Visibility.Hidden; comboBox3.Visibility = Visibility.Hidden; comboBox4.Visibility = Visibility.Hidden; comboBox5.Visibility = Visibility.Hidden; comboBox6.Visibility = Visibility.Hidden; comboBox7.Visibility = Visibility.Hidden; comboBox8.Visibility = Visibility.Hidden; comboBox9.Visibility = Visibility.Hidden; comboBox10.Visibility = Visibility.Hidden; comboBox11.Visibility = Visibility.Hidden; comboBox12.Visibility = Visibility.Hidden;
                    textBox1.Visibility = Visibility.Hidden; textBox2.Visibility = Visibility.Hidden; textBox3.Visibility = Visibility.Hidden; textBox4.Visibility = Visibility.Hidden; textBox5.Visibility = Visibility.Hidden; textBox6.Visibility = Visibility.Hidden; textBox7.Visibility = Visibility.Hidden; textBox8.Visibility = Visibility.Hidden;
                    textBox1.Text = ""; textBox2.Text = ""; textBox3.Text = ""; textBox4.Text = ""; textBox5.Text = ""; textBox6.Text = ""; textBox7.Text = ""; textBox8.Text = "";
                    comboBoxSync.Items.Clear();
                    Application.Current.MainWindow.Width = 745;

                    comboBox1.Visibility = Visibility.Visible;
                    comboBox2.Visibility = Visibility.Visible;
                    comboBox3.Visibility = Visibility.Visible;
                    comboBox4.Visibility = Visibility.Visible;
                    comboBox5.Visibility = Visibility.Visible;
                    comboBox6.Visibility = Visibility.Visible;
                    comboBox7.Visibility = Visibility.Visible;
                    comboBox8.Visibility = Visibility.Visible;
                    comboBox9.Visibility = Visibility.Visible;
                    comboBox10.Visibility = Visibility.Visible;
                    comboBox11.Visibility = Visibility.Visible;
                    comboBox12.Visibility = Visibility.Visible;

                    comboBoxSync.Items.Add(1);
                    comboBoxSync.Items.Add(2);
                    comboBoxSync.Items.Add(3);
                    comboBoxSync.Items.Add(4);
                    comboBoxSync.Items.Add(5);
                    comboBoxSync.Items.Add(6);
                    comboBoxSync.Items.Add(7);
                    comboBoxSync.Items.Add(8);
                    comboBoxSync.Items.Add(9);
                    comboBoxSync.Items.Add(10);
                    comboBoxSync.Items.Add(11);
                    comboBoxSync.Items.Add(12);
                }
            }
        }

        private void textBox1_KeyUp(object sender, KeyEventArgs e)
        {
            if (textBox2.Visibility == Visibility.Visible)
            {
                Keyboard.Focus(textBox2);
            }
            else
            {
                Keyboard.Focus(textBox1);
            }
        }

        private void textBox2_KeyUp(object sender, KeyEventArgs e)
        {
            if (textBox3.Visibility == Visibility.Visible)
            {
                Keyboard.Focus(textBox3);
            }
            else
            {
                Keyboard.Focus(textBox1);
            }
        }

        private void textBox3_KeyUp(object sender, KeyEventArgs e)
        {
            if (textBox4.Visibility == Visibility.Visible)
            {
                Keyboard.Focus(textBox4);
            }
            else
            {
                Keyboard.Focus(textBox1);
            }
        }

        private void textBox4_KeyUp(object sender, KeyEventArgs e)
        {
            if (textBox5.Visibility == Visibility.Visible)
            {
                Keyboard.Focus(textBox5);
            }
            else
            {
                Keyboard.Focus(textBox1);
            }
        }

        private void textBox5_KeyUp(object sender, KeyEventArgs e)
        {
            if (textBox6.Visibility == Visibility.Visible)
            {
                Keyboard.Focus(textBox6);
            }
            else
            {
                Keyboard.Focus(textBox1);
            }
        }

        private void textBox6_KeyUp(object sender, KeyEventArgs e)
        {
            if (textBox7.Visibility == Visibility.Visible)
            {
                Keyboard.Focus(textBox7);
            }
            else
            {
                Keyboard.Focus(textBox1);
            }
        }

        private void textBox7_KeyUp(object sender, KeyEventArgs e)
        {
            if (textBox8.Visibility == Visibility.Visible)
            {
                Keyboard.Focus(textBox8);
            }
            else
            {
                Keyboard.Focus(textBox1);
            }
        }

        private void textBox8_KeyUp(object sender, KeyEventArgs e)
        {
            Keyboard.Focus(textBox1);
        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (textBox1.Text.Length > 0)
            {
                textBox1.Text = "";
            }
        }

        private void textBox2_KeyDown(object sender, KeyEventArgs e)
        {
            if (textBox2.Text.Length > 0)
            {
                textBox2.Text = "";
            }
        }

        private void textBox3_KeyDown(object sender, KeyEventArgs e)
        {
            if (textBox3.Text.Length > 0)
            {
                textBox3.Text = "";
            }
        }

        private void textBox4_KeyDown(object sender, KeyEventArgs e)
        {
            if (textBox4.Text.Length > 0)
            {
                textBox4.Text = "";
            }
        }

        private void textBox5_KeyDown(object sender, KeyEventArgs e)
        {
            if (textBox5.Text.Length > 0)
            {
                textBox5.Text = "";
            }
        }

        private void textBox6_KeyDown(object sender, KeyEventArgs e)
        {
            if (textBox6.Text.Length > 0)
            {
                textBox6.Text = "";
            }
        }

        private void textBox7_KeyDown(object sender, KeyEventArgs e)
        {
            if (textBox7.Text.Length > 0)
            {
                textBox7.Text = "";
            }
        }

        private void textBox8_KeyDown(object sender, KeyEventArgs e)
        {
            if (textBox8.Text.Length > 0)
            {
                textBox8.Text = "";
            }
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            WindowSingle ws = new WindowSingle();
            ws.Show();
        }

        private void comboBoxSync_DropDownClosed(object sender, EventArgs e)
        {
            Keyboard.Focus(textBox1);
        }
    }
}
