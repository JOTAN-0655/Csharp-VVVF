using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VVVF_Generator_Porting
{
    public class vvvf_wave
    {

		static double M_2PI = 6.28318530717958;
        static double M_PI = 3.14159265358979;
		static double M_PI_2 = 1.57079632679489661923;
		static double M_2_PI = 0.636619772367581343076;

        public struct Wave_Values
        {

            public double sin_value;
			public double saw_value;
			public double pwm_value;
        };

		public enum Pulse_Mode
        {
            Not_In_Sync, P_1, P_Wide_3, P_10, P_12, P_18, P_3, P_5, P_7, P_9, P_11, P_13, P_15, P_17, P_19,
            P_21, P_23, P_25, P_27, P_29, P_31, P_33, P_35, P_37, P_39, P_41
            , P_43, P_45, P_47, P_49, P_51, P_53, P_55, P_57, P_59, P_61
        };

        //function caliculation
        public static double get_saw_value_simple(double x)
		{
			double fixed_x = x - (double)((int)(x / M_2PI) * M_2PI);
			if (0 <= fixed_x && fixed_x < M_PI_2)
				return M_2_PI * fixed_x;
			else if (M_PI_2 <= fixed_x && fixed_x < 3.0 * M_PI_2)
				return -M_2_PI * fixed_x + 2;
			else
				return M_2_PI * fixed_x - 4;
		}

		public static double get_saw_value(double time, double angle_frequency, double initial_phase)
		{
			return -get_saw_value_simple(time * angle_frequency + initial_phase);
		}

		public static double get_sin_value(double time, double angle_frequency, double initial_phase, double amplitude)
		{
			return my_math.sin(time * angle_frequency + initial_phase) * amplitude;
		}

		public static double get_pwm_value(double sin_value, double saw_value)
		{
			if (disconnect) return 0;
			if (sin_value - saw_value > 0)
				return 1;
			else
				return -1;
		}

		public static Wave_Values get_Wide_P_3(double time, double angle_frequency, double initial_phase, double voltage)
		{
			double sin = get_sin_value(time, angle_frequency, initial_phase, 1);
			double saw = get_saw_value(time, angle_frequency, initial_phase);
			double pwm = ((sin - saw > 0) ? 1 : -1) * voltage;
			double nega_saw = (saw > 0) ? saw - 1 : saw + 1;
			double gate = get_pwm_value(pwm, nega_saw);
			Wave_Values wv = new Wave_Values();
			wv.sin_value = pwm;
			wv.saw_value = nega_saw;
			wv.pwm_value = gate;
			return wv;
		}

		public static Wave_Values get_P_with_saw(double time, double sin_angle_frequency, double initial_phase, double voltage, double carrier_mul)
		{
			double carrier_saw = -get_saw_value(time, carrier_mul * sin_angle_frequency, carrier_mul * initial_phase);
			double saw = -get_saw_value(time, sin_angle_frequency, initial_phase);
			double pwm = (saw > 0) ? voltage : -voltage;
			double gate = get_pwm_value(pwm, carrier_saw);
			Wave_Values wv;
			wv.sin_value = saw;
			wv.saw_value = carrier_saw;
			wv.pwm_value = gate;
			return wv;
		}

		public static double get_Amplitude(double freq, double max_freq)
		{

			double rate = 0.99, init = 0.01;
			if (freq > max_freq)
				return 1.0;
			if (freq <= 0.1)
				return 0.0;

			return rate / max_freq * freq + init;
		}

		public static int get_Pulse_Num(Pulse_Mode mode)
		{
			if (mode == Pulse_Mode.Not_In_Sync)
				return -1;
			if (mode == Pulse_Mode.P_1)
				return 0;
			if (mode == Pulse_Mode.P_Wide_3)
				return 0;
			if (mode == Pulse_Mode.P_5)
				return 6;
			if (mode == Pulse_Mode.P_7)
				return 9;
			if (mode == Pulse_Mode.P_10)
				return 10;
			if (mode == Pulse_Mode.P_12)
				return 12;
			if (mode == Pulse_Mode.P_18)
				return 18;
			return 3 + (2 * ((int)mode - 6));
		}

		//sin value definitions
		public static double sin_angle_freq = 0;
		public static double sin_time = 0;

		//saw value definitions
		public static double saw_angle_freq = 1050;
		public static double saw_time = 0;
		public static double pre_saw_random_freq = 0;

		public static bool disconnect = false;

		public static int random_freq_move_count = 0;

		public static void reset_all_variables()
		{
			sin_angle_freq = 0;
			sin_time = 0;

			//saw value definitions
			saw_angle_freq = 1050;
			saw_time = 0;

			random_freq_move_count = 0;
		}

		public static Wave_Values calculate_common(Pulse_Mode pulse_mode, double expect_saw_angle_freq, double initial_phase, double amplitude)
		{

			if (pulse_mode == Pulse_Mode.P_Wide_3)
				return get_Wide_P_3(sin_time, sin_angle_freq, initial_phase, amplitude);
			if (pulse_mode == Pulse_Mode.P_5)
				return get_P_with_saw(sin_time, sin_angle_freq, initial_phase, amplitude, get_Pulse_Num(pulse_mode));
			if (pulse_mode == Pulse_Mode.P_7)
				return get_P_with_saw(sin_time, sin_angle_freq, initial_phase, amplitude, get_Pulse_Num(pulse_mode));

			if (pulse_mode == Pulse_Mode.Not_In_Sync)
				saw_time = saw_angle_freq / expect_saw_angle_freq * saw_time;
			else
			{
				expect_saw_angle_freq = sin_angle_freq * get_Pulse_Num(pulse_mode);
				saw_time = sin_time;
			}
			saw_angle_freq = expect_saw_angle_freq;

			double sin_value = get_sin_value(sin_time, sin_angle_freq, initial_phase, amplitude);
			double saw_value = get_saw_value(saw_time, saw_angle_freq, 0);
			double pwm_value = get_pwm_value(sin_value, saw_value);

			Wave_Values wv;
			wv.sin_value = sin_value;
			wv.saw_value = saw_value;
			wv.pwm_value = pwm_value;
			return wv;
		}

		public static Wave_Values calculate_E231(bool brake, double initial_phase)
		{
			double sin_freq = sin_angle_freq / 2.0 / M_PI;
			double amplitude = get_Amplitude(sin_freq, 65);
			double expect_saw_angle_freq = 0;
			Pulse_Mode pulse_mode;
			if (sin_freq > 67)
				pulse_mode = Pulse_Mode.P_1;
			else if (sin_freq > 60)
				pulse_mode = Pulse_Mode.P_Wide_3;
			else if (49 <= sin_freq && sin_freq <= 60)
			{
				double expect_saw_freq = 710 + (1750 - 710) / 11 * (sin_freq - 49);
				expect_saw_angle_freq = M_2PI * expect_saw_freq;
				pulse_mode = Pulse_Mode.Not_In_Sync;
			}
			else if (23 <= sin_freq && sin_freq < 50)
			{
				double expect_saw_freq = 1045 + (710 - 1045) / 26 * (sin_freq - 23);
				expect_saw_angle_freq = M_2PI * expect_saw_freq;
				pulse_mode = Pulse_Mode.Not_In_Sync;
			}
			else
			{
				expect_saw_angle_freq = 1045 * M_2PI;
				pulse_mode = Pulse_Mode.Not_In_Sync;
			}

			return calculate_common(pulse_mode, expect_saw_angle_freq, initial_phase, amplitude);
		}

		public static Wave_Values calculate_207(bool brake, double initial_phase)
		{
			double sin_freq = sin_angle_freq / M_2PI;

			double amplitude = get_Amplitude(sin_freq, 60);

			double expect_saw_angle_freq = 0;
			Pulse_Mode pulse_mode;
			if (60 <= sin_freq)
				pulse_mode = Pulse_Mode.P_1;
			else if (53 <= sin_freq)
				pulse_mode = Pulse_Mode.P_3;
			else if (44 <= sin_freq)
				pulse_mode = Pulse_Mode.P_5;
			else if (31 <= sin_freq)
				pulse_mode = Pulse_Mode.P_10;
			else if (14 <= sin_freq)
				pulse_mode = Pulse_Mode.P_15;
			else if (sin_freq < 14 && !brake)
			{
				expect_saw_angle_freq = M_2PI * 365;
				pulse_mode = Pulse_Mode.Not_In_Sync;
			}
			else if (8 < sin_freq && sin_freq < 14 && brake)
			{

				expect_saw_angle_freq = sin_angle_freq * 27;
				pulse_mode = Pulse_Mode.P_27;
				saw_time = sin_time;
			}
			else
			{
				Wave_Values wv;
				wv.sin_value = 0;
				wv.saw_value = 0;
				wv.pwm_value = 0;
				return wv;
			}
			return calculate_common(pulse_mode, expect_saw_angle_freq, initial_phase, amplitude);
		}

		public static Wave_Values calculate_doremi(bool brake, double initial_phase)
		{
			double sin_freq = sin_angle_freq / M_2PI;

			double amplitude = get_Amplitude(sin_freq, 80);

			double expect_saw_angle_freq = 0;
			Pulse_Mode pulse_mode;
			if (80 <= sin_freq)
				pulse_mode = Pulse_Mode.P_1;
			else if (57 <= sin_freq)
				pulse_mode = Pulse_Mode.P_Wide_3;
			else if (50 <= sin_freq)
				pulse_mode = Pulse_Mode.P_3;
			else if (43 <= sin_freq)
				pulse_mode = Pulse_Mode.P_5;
			else if (35 <= sin_freq)
				pulse_mode = Pulse_Mode.P_7;
			else if (30 <= sin_freq)
				pulse_mode = Pulse_Mode.P_12;
			else if (27 <= sin_freq)
				pulse_mode = Pulse_Mode.P_15;
			else if (24.5 <= sin_freq)
				pulse_mode = Pulse_Mode.P_18;
			else
			{
				if (!brake)
				{
					if (5.6 <= sin_freq)
					{
						expect_saw_angle_freq = 2 * M_PI * 400;
						pulse_mode = Pulse_Mode.Not_In_Sync;
					}
					else if (5 <= sin_freq)
					{
						expect_saw_angle_freq = 2 * M_PI * 350;
						pulse_mode = Pulse_Mode.Not_In_Sync;
					}
					else if (4.3 <= sin_freq)
					{
						expect_saw_angle_freq = 2 * M_PI * 311;
						pulse_mode = Pulse_Mode.Not_In_Sync;
					}
					else if (3.4 <= sin_freq)
					{
						expect_saw_angle_freq = 2 * M_PI * 294;
						pulse_mode = Pulse_Mode.Not_In_Sync;
					}
					else if (2.7 <= sin_freq)
					{
						expect_saw_angle_freq = 2 * M_PI * 262;
						pulse_mode = Pulse_Mode.Not_In_Sync;
					}
					else if (2.0 <= sin_freq)
					{
						expect_saw_angle_freq = 2 * M_PI * 233;
						pulse_mode = Pulse_Mode.Not_In_Sync;
					}
					else if (1.5 <= sin_freq)
					{
						expect_saw_angle_freq = 2 * M_PI * 223;
						pulse_mode = Pulse_Mode.Not_In_Sync;
					}
					else if (0.5 <= sin_freq)
					{
						expect_saw_angle_freq = 2 * M_PI * 196;
						pulse_mode = Pulse_Mode.Not_In_Sync;
					}
					else
					{
						expect_saw_angle_freq = 2 * M_PI * 175;
						pulse_mode = Pulse_Mode.Not_In_Sync;
					}
				}
				else
				{
					if (sin_freq > 4)
					{
						expect_saw_angle_freq = 2 * M_PI * 400;
						pulse_mode = Pulse_Mode.Not_In_Sync;
					}
					else
					{
						Wave_Values wv;
						wv.sin_value = 0;
						wv.saw_value = 0;
						wv.pwm_value = 0;
						return wv;
					}
				}
			}

			return calculate_common(pulse_mode, expect_saw_angle_freq, initial_phase, amplitude);
		}

		public static Wave_Values calculate_E235(bool brake, double initial_phase)
		{
			double sin_freq = sin_angle_freq / M_2PI;

			double amplitude = get_Amplitude(sin_freq, 54);

			double sin_value = get_sin_value(sin_time, sin_angle_freq, initial_phase, amplitude);
			double saw_value, pwm_value;
			if (sin_freq > 54)
			{

				saw_angle_freq = sin_angle_freq * 15;
				saw_time = sin_time;

				saw_value = get_saw_value(saw_time, saw_angle_freq, 0);
			}
			else
			{

				if (random_freq_move_count == 0)
				{
					//saw_freq = 740;
					int random_v = my_math.my_random();
					int diff_freq = my_math.mod_i(random_v, 100);
					if (my_math.mod_i(random_v, 500) < 250)
						diff_freq = -diff_freq;

					double base_freq = (double)550 + 3.148148148148148 * (sin_freq); //170.0/54.0*(sin_freq);

					double silent_random_freq = base_freq + diff_freq;

					double expect_saw_angle_freq = 2 * M_PI * silent_random_freq;
					saw_time = saw_angle_freq / expect_saw_angle_freq * saw_time;
					saw_angle_freq = expect_saw_angle_freq;
				}
				saw_value = get_saw_value(saw_time, saw_angle_freq, 0);

				random_freq_move_count++;
				if (random_freq_move_count == 30)
					random_freq_move_count = 0;
			}

			pwm_value = get_pwm_value(sin_value, saw_value);

			Wave_Values wv;
			wv.sin_value = sin_value;
			wv.saw_value = saw_value;
			wv.pwm_value = pwm_value;
			return wv;
		}

		public static Wave_Values calculate_E209(bool brake, double initial_phase)
		{
			double sin_freq = sin_angle_freq / M_2PI;
			double amplitude = get_Amplitude(sin_freq, 53);

			double expect_saw_angle_freq = sin_angle_freq * 10;
			Pulse_Mode pulse_mode;
			if (53 <= sin_freq)
				pulse_mode = Pulse_Mode.P_1;
			else if (46 <= sin_freq)
				pulse_mode = Pulse_Mode.P_3;
			else if (30 <= sin_freq)
				pulse_mode = Pulse_Mode.P_9;
			else if (19 <= sin_freq)
				pulse_mode = Pulse_Mode.P_21;
			else if (9 <= sin_freq)
				pulse_mode = Pulse_Mode.P_33;
			else if (2 <= sin_freq && !brake)
				pulse_mode = Pulse_Mode.P_57;
			else if (sin_freq < 2 && !brake)
			{
				pulse_mode = Pulse_Mode.Not_In_Sync;
				expect_saw_angle_freq = M_2PI * 114;
			}
			else if (8 < sin_freq && sin_freq < 18 && brake)
				pulse_mode = Pulse_Mode.P_33;
			else
			{
				Wave_Values wv;
				wv.sin_value = 0;
				wv.saw_value = 0;
				wv.pwm_value = 0;
				return wv;
			}
			return calculate_common(pulse_mode, expect_saw_angle_freq, initial_phase, amplitude);
		}

		public static Wave_Values calculate_9820_mitsubishi(bool brake, double initial_phase)
		{
			double sin_freq = sin_angle_freq / M_2PI;

			double amplitude = get_Amplitude(sin_freq, 55);

			double expect_saw_angle_freq = sin_angle_freq * 10;
			Pulse_Mode pulse_Mode = Pulse_Mode.P_1;
			if (55 <= sin_freq)
				pulse_Mode = Pulse_Mode.P_1;
			else if (50 <= sin_freq)
				pulse_Mode = Pulse_Mode.P_3;
			else if (13 <= sin_freq)
			{
				expect_saw_angle_freq = 700 * M_2PI;
				pulse_Mode = Pulse_Mode.Not_In_Sync;
			}
			else if (brake && sin_freq < 8.5)
			{
				Wave_Values wv;
				wv.sin_value = 0;
				wv.saw_value = 0;
				wv.pwm_value = 0;
				return wv;
			}
			else if (sin_freq > 2)
			{
				double expect_saw_freq = 250 + (700 - 250) / 11 * (sin_freq - 2);
				expect_saw_angle_freq = M_2PI * expect_saw_freq;
				pulse_Mode = Pulse_Mode.Not_In_Sync;
			}
			else
			{
				expect_saw_angle_freq = 250 * M_2PI;
				pulse_Mode = Pulse_Mode.Not_In_Sync;
			}

			return calculate_common(pulse_Mode, expect_saw_angle_freq, initial_phase, amplitude);
		}

		public static Wave_Values calculate_9820_hitachi(bool brake, double initial_phase)
		{
			double sin_freq = sin_angle_freq / M_2PI;

			double amplitude = get_Amplitude(sin_freq, 65);

			double expect_saw_angle_freq = sin_angle_freq * 10;
			Pulse_Mode pulse_Mode = Pulse_Mode.P_1;
			if (67 <= sin_freq)
				pulse_Mode = Pulse_Mode.P_1;
			else if (60 <= sin_freq)
				pulse_Mode = Pulse_Mode.P_Wide_3;
			else if (49 <= sin_freq)
			{
				double expect_saw_freq = 780 + (1820 - 780) / 11 * (sin_freq - 49);
				expect_saw_angle_freq = expect_saw_freq * M_2PI;
				pulse_Mode = Pulse_Mode.Not_In_Sync;
			}
			else
			{
				expect_saw_angle_freq = 780 * M_2PI;
				pulse_Mode = Pulse_Mode.Not_In_Sync;
			}

			return calculate_common(pulse_Mode, expect_saw_angle_freq, initial_phase, amplitude);
		}

		public static Wave_Values calculate_E233(bool brake, double initial_phase)
		{

			double sin_freq = sin_angle_freq / M_2PI;

			double amplitude = get_Amplitude(sin_freq, 50);
			double expect_saw_angle_freq = 1;
			Pulse_Mode pulse_Mode = Pulse_Mode.P_1;
			if (50 <= sin_freq)
				pulse_Mode = Pulse_Mode.P_1;
			else if (45 <= sin_freq)
				pulse_Mode = Pulse_Mode.P_3;
			else
			{
				pulse_Mode = Pulse_Mode.Not_In_Sync;
				if (random_freq_move_count == 0 || pre_saw_random_freq == 0)
				{
					int random_v = my_math.my_random();
					int diff_freq = my_math.mod_i(random_v, 100);
					if (my_math.mod_i(random_v, 500) < 250)
						diff_freq = -diff_freq;
					double silent_random_freq = 750 + diff_freq;

					expect_saw_angle_freq = M_2PI * silent_random_freq;
					pre_saw_random_freq = expect_saw_angle_freq;
				}
				else
				{
					expect_saw_angle_freq = pre_saw_random_freq;
				}
				random_freq_move_count++;
				if (random_freq_move_count == 30)
					random_freq_move_count = 0;
			}

			return calculate_common(pulse_Mode, expect_saw_angle_freq, initial_phase, amplitude);
		}

		public static Wave_Values calculate_silent(bool brake, double initial_phase)
		{
			double sin_freq = sin_angle_freq / M_2PI;

			double amplitude = get_Amplitude(sin_freq, 60);
			double expect_saw_angle_freq = 1;
			Pulse_Mode pulse_Mode = Pulse_Mode.P_1;
			if (50 <= sin_freq)
				pulse_Mode = Pulse_Mode.P_27;
			else
			{
				pulse_Mode = Pulse_Mode.Not_In_Sync;
				if (random_freq_move_count == 0 || pre_saw_random_freq == 0)
				{
					int random_v = my_math.my_random();
					int diff_freq = my_math.mod_i(random_v, 100);
					if (my_math.mod_i(random_v, 500) < 250)
						diff_freq = -diff_freq;
					double silent_random_freq = 550 + diff_freq;

					expect_saw_angle_freq = M_2PI * silent_random_freq;
					pre_saw_random_freq = expect_saw_angle_freq;
				}
				else
				{
					expect_saw_angle_freq = pre_saw_random_freq;
				}
				random_freq_move_count++;
				if (random_freq_move_count == 30)
					random_freq_move_count = 0;
			}

			return calculate_common(pulse_Mode, expect_saw_angle_freq, initial_phase, amplitude);
		}

		public static Wave_Values calculate_mitsubishi_gto(bool brake, double initial_phase)
		{
			double sin_freq = sin_angle_freq / M_2PI;

			double amplitude = get_Amplitude(sin_freq, 63);

			double expect_saw_angle_freq = sin_angle_freq * 10;
			Pulse_Mode pulse_Mode = Pulse_Mode.P_1;
			if (63 <= sin_freq)
				pulse_Mode = Pulse_Mode.P_1;
			else if (60 <= sin_freq)
				pulse_Mode = Pulse_Mode.P_Wide_3;
			else if (57 <= sin_freq)
				pulse_Mode = Pulse_Mode.P_3;
			else if (44 <= sin_freq)
				pulse_Mode = Pulse_Mode.P_5;
			else if (36 <= sin_freq)
				pulse_Mode = Pulse_Mode.P_7;
			else if (16 <= sin_freq)
			{
				expect_saw_angle_freq = 400 * M_2PI;
				pulse_Mode = Pulse_Mode.Not_In_Sync;
			}
			else if (brake && sin_freq < 7.4)
			{
				Wave_Values wv;
				wv.sin_value = 0;
				wv.saw_value = 0;
				wv.pwm_value = 0;
				return wv;
			}
			else if (sin_freq >= 2)
			{
				double expect_saw_freq = 216 + (400 - 216) / 14 * (sin_freq - 2);
				expect_saw_angle_freq = M_2PI * expect_saw_freq;
				pulse_Mode = Pulse_Mode.Not_In_Sync;
			}
			else
			{
				expect_saw_angle_freq = 216 * M_2PI;
				pulse_Mode = Pulse_Mode.Not_In_Sync;
			}

			return calculate_common(pulse_Mode, expect_saw_angle_freq, initial_phase, amplitude);
		}

		public static Wave_Values calculate_toyo_IGBT(bool brake, double initial_phase)
		{
			double sin_freq = sin_angle_freq / M_2PI;

			double amplitude = get_Amplitude(sin_freq, 55);

			if (53 <= sin_freq && sin_freq <= 55)
			{
				amplitude = 5 + (get_Amplitude(53, 55) - 5) / 2.0 * (55 - sin_freq);
			}

			double expect_saw_angle_freq = sin_angle_freq * 10;
			Pulse_Mode pulse_Mode = Pulse_Mode.P_1;
			if (55 <= sin_freq)
				pulse_Mode = Pulse_Mode.P_1;
			else if (34 <= sin_freq)
				pulse_Mode = Pulse_Mode.P_9;
			else
			{
				expect_saw_angle_freq = 1045 * M_2PI;
				pulse_Mode = Pulse_Mode.Not_In_Sync;
			}

			return calculate_common(pulse_Mode, expect_saw_angle_freq, initial_phase, amplitude);
		}

		public static Wave_Values calculate_Famima(bool brake, double initial_phase)
		{
			double sin_freq = sin_angle_freq / M_2PI;

			double amplitude = get_Amplitude(sin_freq, 60);

			double expect_saw_angle_freq = 0;
			Pulse_Mode pulse_mode;
			if (60 <= sin_freq)
				pulse_mode = Pulse_Mode.P_1;
			else if (56 <= sin_freq)
				pulse_mode = Pulse_Mode.P_3;
			else
			{
				double expect_saw_freq = 0;
				if (48 <= sin_freq)
					expect_saw_freq = 622;
				else if (44 <= sin_freq)
					expect_saw_freq = 466;
				else if (40 <= sin_freq)
					expect_saw_freq = 698;
				else if (36 <= sin_freq)
					expect_saw_freq = 783;
				else if (32 <= sin_freq)
					expect_saw_freq = 698;
				else if (28 <= sin_freq)
					expect_saw_freq = 466;
				else if (20 <= sin_freq)
					expect_saw_freq = 932;
				else if (16 <= sin_freq)
					expect_saw_freq = 587;
				else if (12 <= sin_freq)
					expect_saw_freq = 622;
				else if (8 <= sin_freq)
					expect_saw_freq = 466;
				else if (4 <= sin_freq)
					expect_saw_freq = 622;
				else
					expect_saw_freq = 783;

				expect_saw_angle_freq = M_2PI * expect_saw_freq;
				pulse_mode = Pulse_Mode.Not_In_Sync;
			}

			return calculate_common(pulse_mode, expect_saw_angle_freq, initial_phase, amplitude);
		}

		public static Wave_Values calculate_real_doremi(bool brake, double initial_phase)
		{
			double sin_freq = sin_angle_freq / M_2PI;

			double amplitude = get_Amplitude(sin_freq, 80);

			double expect_saw_angle_freq = 0;
			Wave_Values wv;
			Pulse_Mode pulse_mode;
			if (80 <= sin_freq)
				pulse_mode = Pulse_Mode.P_1;
			else if (57 <= sin_freq)
				pulse_mode = Pulse_Mode.P_Wide_3;
			else if (50 <= sin_freq)
				pulse_mode = Pulse_Mode.P_3;
			else if (43 <= sin_freq)
				pulse_mode = Pulse_Mode.P_5;
			else if (35 <= sin_freq)
				pulse_mode = Pulse_Mode.P_7;
			else if (30 <= sin_freq)
				pulse_mode = Pulse_Mode.P_12;
			else if (27 <= sin_freq)
				pulse_mode = Pulse_Mode.P_15;
			else if (24.5 <= sin_freq)
				pulse_mode = Pulse_Mode.P_18;
			else
			{
				if (!brake)
				{
					double expect_saw_freq = 0;
					if (5.6 <= sin_freq)
						expect_saw_freq = 587;
					else if (5 <= sin_freq)
						expect_saw_freq = 523;
					else if (4.3 <= sin_freq)
						expect_saw_freq = 493;
					else if (3.4 <= sin_freq)
						expect_saw_freq = 440;
					else if (2.7 <= sin_freq)
						expect_saw_freq = 391;
					else if (2.0 <= sin_freq)
						expect_saw_freq = 349;
					else if (1.5 <= sin_freq)
						expect_saw_freq = 329;
					else if (0.5 <= sin_freq)
						expect_saw_freq = 293;
					else
						expect_saw_freq = 261;
					expect_saw_angle_freq = M_2PI * expect_saw_freq;
					pulse_mode = Pulse_Mode.Not_In_Sync;
				}
				else
				{
					if (sin_freq > 4)
					{
						expect_saw_angle_freq = M_2PI * 400;
						pulse_mode = Pulse_Mode.Not_In_Sync;
					}
					else
					{
						wv.sin_value = 0;
						wv.saw_value = 0;
						wv.pwm_value = 0;
						return wv;
					}
				}
			}

			return calculate_common(pulse_mode, expect_saw_angle_freq, initial_phase, amplitude);
		}

		public static Wave_Values calculate_toubu_50050(bool brake, double initial_phase)
		{

			double sin_freq = sin_angle_freq / M_2PI;

			double amplitude = get_Amplitude(sin_freq, 61);
			double expect_saw_angle_freq = 1;
			Pulse_Mode pulse_Mode = Pulse_Mode.P_1;
			if (61 <= sin_freq)
				pulse_Mode = Pulse_Mode.P_1;
			else if (58 <= sin_freq) pulse_Mode = Pulse_Mode.P_Wide_3;
			else if (49 <= sin_freq)
			{
				pulse_Mode = Pulse_Mode.Not_In_Sync;
				double base_freq = (double)680 + 1140 / 9.0 * (sin_freq - 49); //170.0/54.0*(sin_freq);
				expect_saw_angle_freq = M_2PI * base_freq;
			}
			else if (46 <= sin_freq)
            {
				pulse_Mode = Pulse_Mode.Not_In_Sync;
				double base_freq = (double)730 - 50.0 / 49.0 * (sin_freq); //170.0/54.0*(sin_freq);
				expect_saw_angle_freq = M_2PI * base_freq;
			}
			else
			{
				pulse_Mode = Pulse_Mode.Not_In_Sync;
				if (random_freq_move_count == 0 || pre_saw_random_freq == 0)
				{
					int random_v = my_math.my_random();
					int diff_freq = my_math.mod_i(random_v, 100);
					if (my_math.mod_i(random_v, 500) < 250)
						diff_freq = -diff_freq;

					double base_freq = (double)730 - 50.0 / 49.0 * (sin_freq); //170.0/54.0*(sin_freq);

					double silent_random_freq = base_freq + diff_freq;

					expect_saw_angle_freq = M_2PI * silent_random_freq;
					pre_saw_random_freq = expect_saw_angle_freq;
				}
				else
				{
					expect_saw_angle_freq = pre_saw_random_freq;
				}
				random_freq_move_count++;
				if (random_freq_move_count == 30)
					random_freq_move_count = 0;
			}

			return calculate_common(pulse_Mode, expect_saw_angle_freq, initial_phase, amplitude);
		}
	}
}
