using System;

namespace Framework.Utils
{	
	public class MoneyUtils
	{
		public static string MoneyToString(decimal data)
		{
			if(data<=0 || data > 1000000000000000) return string.Empty;

			string[]   Kop = {"копейка", "копейки", "копеек" };
			string rez = RublesToString(data);
												
			if(rez != string.Empty)
			{
				//Для копеек!
				int _decades = (int)((data%1)*10); //Кол-во десятков				
				int _units = ((int)((data%1)*100))%10; //Кол-во едениц

				int number = 2 ; //По умолчанию копеек

				if(_decades != 1)
				{
					if(_units == 1) number = 0;
					else
						if(_units > 1 && _units <5) number = 1;				
				}
				rez += _decades;
				rez += _units + " " + Kop[number];
			}
			return rez;			
		}

		public static string RublesToString(decimal data)
		{			
			if(data<=0  || data > 1000000000000000) return string.Empty;			

			string[] Rub = {"рубль ", "рубля ", "рублей "};
			string rez = NumberToString(data);

			if(rez != string.Empty)
			{
				//Для рублей!
				int _decades = (int)((data%100)/10); //Кол-во десятков
				int _units = (int)(data%10); //Кол-во едениц

				int number = 2 ; //По умолчанию рублей

				if(_decades != 1)
				{
					if(_units == 1) number = 0;
					else
						if(_units > 1 && _units <5) number = 1;				
				}			
				rez +=Rub[number];
			}
			return rez;	

		}

		public static string NumberToString(decimal data)
		{
			string rez = string.Empty;
			if(data<=0 || data > 1000000000000000) return rez;			
						
			// Названия групп по тысяче (по три десятичных полядка)
			string[] Gname = {"триллион ", "триллиона ", "триллионов ",
								 "миллиард ", "миллиарда ", "миллиардов ",
								 "миллион ",  "миллиона ",  "миллионов ",
								 "тысяча ",   "тысячи ",    "тысяч ",
								 "",          "",           ""};

			// Сотни
			string[] Hundreds = {"", "сто ", "двести ", "триста ", "четыреста ", "пятьсот ",
									"шестьсот ", "семьсот ", "восемьсот ", "девятьсот "};

			// Десятки
			string[] Decades = {"", "", "двадцать ", "тридцать ", "сорок ", "пятьдесят ",
								   "шестьдесят ", "семьдесят ", "восемьдесят ", "девяносто "};

			// Десять +
			string[] Ten = {"десять ", "одиннадцать ", "двенадцать ", "тринадцать ",
							   "четырнадцать ", "пятнадцать ", "шестнадцать ",
							   "семнадцать ", "восемнадцать ", "девятнадцать "};
							
			// Еденицы
			string[] Units = {"", "один ", "два ", "три ", "четыре ", "пять ",
								 "шесть ", "семь ", "восемь ", "девять " };

			// Исключения для тысяч (женского рода)
			string[] Unitsf = { "", "одна ", "две " };
			
			int _hundreds,_decades,_units,_data,number;
			decimal p1,p2;
			
			for(int i=0;i<5;i++)
			{
				//Устновка числа от 0 до 999				
				p2 = (decimal)(Math.Pow(10,12.0 - i*3.0));

				if(i!=0)
				{
					p1 = (decimal)(Math.Pow(10.0,15.0 - i*3.0));
					_data = (int)((data%p1)/p2);
				}
				else _data = (int)(data/p2);

				if(_data != 0)
				{
					_hundreds = _data / 100;
					_decades = (_data%100)/10;
					_units = _data%10;
					rez+=Hundreds[_hundreds];
					rez+=Decades[_decades];
				
					number = 2 ; //По умолчанию
					if(_decades != 1)
					{
						if(_units == 1) number = 0;
						else
							if(_units > 1 && _units <5) number = 1;

						if(i==3 && _units <= 2) rez+=Unitsf[_units];
						else rez+=Units[_units];
					}
					else
						rez+=Ten[_units];

					rez+=Gname[number+i*3];
				}
			}
			return rez;
		}
	}
}
