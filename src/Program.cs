using System; 
using System.IO;
using static VVVF_Generator_Porting.vvvf_wave;

namespace VVVF_Generator_Porting
{
    internal class Program
    {

        static double count = 0;
        static double div_dreq = 40 * 1000;
        static void generate_sound()
        {

            DateTime dt = DateTime.Now;
            String gen_time = dt.ToString("yyyy-MM-dd_HH-mm-ss");


            String fileName = @"save_dir/" + gen_time + ".pcm";

            BinaryWriter writer = new BinaryWriter(new FileStream(fileName, FileMode.Create));

            Boolean do_frequency_change = true;
            Boolean brake = false;

            while (true)
            {
                vvvf_wave.sin_time += 1.00 / div_dreq;
                vvvf_wave.saw_time += 1.00 / div_dreq;

                Wave_Values wv_U = calculate_toubu_50050(brake, Math.PI * 2.0 / 3.0 * 0);
                Wave_Values wv_V = calculate_toubu_50050(brake, Math.PI * 2.0 / 3.0 * 1);


                for (int i = 0; i < 1; i++)
                {
                    if (wv_U.pwm_value - wv_V.pwm_value > 0) writer.Write((byte)0x90);
                    else if (wv_U.pwm_value - wv_V.pwm_value < 0) writer.Write((byte)0x70);
                    else writer.Write((byte)0x80);
                }

                count++;
                if (count % 8 == 0 && do_frequency_change)
                {
                    double sin_new_angle_freq = sin_angle_freq;
                    if (!brake) sin_new_angle_freq += Math.PI / 500;
                    else sin_new_angle_freq -= Math.PI / 500;
                    double amp = sin_angle_freq / sin_new_angle_freq;
                    sin_angle_freq = sin_new_angle_freq;
                    sin_time = amp * sin_time;
                }

                if (sin_angle_freq / 2 / Math.PI > 100 && !brake && do_frequency_change)
                {
                    do_frequency_change = false;
                    count = 0;
                }
                else if (count / div_dreq > 0 && !do_frequency_change)
                {
                    do_frequency_change = true;
                    brake = true;
                }
                else if (sin_angle_freq / 2 / Math.PI < 0 && brake && do_frequency_change) break;

            }

            writer.Close();
        }
        static void Main(string[] args)
        {
            DateTime startDt = DateTime.Now;

            generate_sound();

            DateTime endDt = DateTime.Now;

            TimeSpan ts = endDt - startDt;

            Console.WriteLine("Time took to generate vvvf sound : " + ts.TotalSeconds);
        }
    }
}
