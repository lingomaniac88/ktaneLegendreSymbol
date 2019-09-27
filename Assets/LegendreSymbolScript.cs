using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LegendreSymbolScript : MonoBehaviour
{
	// Used for logging.
	class LegendreSymbol
	{
		public int A { get; private set; }
		public int P { get; private set; }

		public bool IsSquaredTop { get; protected set; }

		public LegendreSymbol(int a, int p)
		{
			A = a;
			P = p;
			IsSquaredTop = false;
		}

		public override string ToString()
		{
			return String.Format("({0}|{1})", A, P);
		}
	}

	// A special Legendre symbol that prints its top value as a perfect square.
	class SquaredTopLegendreSymbol : LegendreSymbol
	{
		public SquaredTopLegendreSymbol(int a, int p) : base(a, p)
		{
			IsSquaredTop = true;
		}

		public override string ToString()
		{
			int sqrtA = (int) Math.Round(Math.Sqrt(A));
			return String.Format("({0}²|{1})", sqrtA, P);
		}
	}

	public KMAudio Audio;
	public KMBombModule BombModule;

	public KMSelectable YesButton;
	public KMSelectable NoButton;

	public TextMesh TopDisplay;
	public TextMesh BottomDisplay;

	bool ExpectedAnswer;
	bool WillAcceptAnswer;

	static int ModuleIdCounter = 1;
	int ModuleId;

	static int[] PrimesBelow1000 = {2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37, 41, 43, 47, 53, 59, 61, 67, 71, 73, 79, 83, 89, 97, 101, 103, 107, 109, 113, 127, 131, 137, 139, 149, 151, 157, 163, 167, 173, 179, 181, 191, 193, 197, 199, 211, 223, 227, 229, 233, 239, 241, 251, 257, 263, 269, 271, 277, 281, 283, 293, 307, 311, 313, 317, 331, 337, 347, 349, 353, 359, 367, 373, 379, 383, 389, 397, 401, 409, 419, 421, 431, 433, 439, 443, 449, 457, 461, 463, 467, 479, 487, 491, 499, 503, 509, 521, 523, 541, 547, 557, 563, 569, 571, 577, 587, 593, 599, 601, 607, 613, 617, 619, 631, 641, 643, 647, 653, 659, 661, 673, 677, 683, 691, 701, 709, 719, 727, 733, 739, 743, 751, 757, 761, 769, 773, 787, 797, 809, 811, 821, 823, 827, 829, 839, 853, 857, 859, 863, 877, 881, 883, 887, 907, 911, 919, 929, 937, 941, 947, 953, 967, 971, 977, 983, 991, 997};

	static bool IsPerfectSquare(int n)
	{
		int roundedSquareRoot = (int) Math.Round(Math.Sqrt(n));
		return roundedSquareRoot * roundedSquareRoot == n;
	}

	// NOTE: This assumes that n < 1000.
	static bool IsPrime(int n)
	{
		return Array.BinarySearch(PrimesBelow1000, n) >= 0;
	}

	// Each key-value pair consists of a prime and its exponent.  We only look
	// at primes less than 1000, which is good enough for our purposes.
	static Dictionary<int, int> PrimeFactorization(int n)
	{
		var answer = new Dictionary<int, int>();

		foreach (int p in PrimesBelow1000)
		{
			int exponent = 0;
			while (n % p == 0)
			{
				exponent++;
				n /= p;
			}

			if (exponent > 0)
			{
				answer[p] = exponent;
			}

			if (n == 1)
			{
				break;
			}
		}

		return answer;
	}

	void Awake()
	{
		ModuleId = ModuleIdCounter++;

		YesButton.OnInteract += delegate () { ButtonDown(true); return false; };
		YesButton.OnInteractEnded += ButtonUp;
		
		NoButton.OnInteract += delegate () { ButtonDown(false); return false; };
		NoButton.OnInteractEnded += ButtonUp;
	}

	void Start()
	{
		BombModule.OnActivate += GenerateValues;
	}

	void ModuleLog(string format, params object[] args)
	{
		var prefix = String.Format("[Legendre Symbol #{0}] ", ModuleId);
		Debug.LogFormat(prefix + format, args);
	}

	void ButtonDown(bool answer)
	{
		ModuleLog("Pressed button \"{0}\"", answer ? "R" : "N");
		Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
		(answer ? YesButton : NoButton).AddInteractionPunch(0.5f);
		
		if (WillAcceptAnswer)
		{
			if (answer == ExpectedAnswer)
			{
				WillAcceptAnswer = false;
				ModuleLog("You pressed the correct button!  Module disarmed.");
				Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
				BombModule.HandlePass();
			}
			else
			{
				ModuleLog("Strike!  You pressed the wrong button.  Generating new numbers...");
				StartCoroutine(PerformStrike());
			}
		}
	}

	void ButtonUp()
	{
		Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonRelease, transform);
	}

	void GenerateValues()
	{
		// There are 25 primes below 100, so skip those
		int bottomValue = PrimesBelow1000[UnityEngine.Random.Range(25, PrimesBelow1000.Length)];
		int topValue = UnityEngine.Random.Range(1, bottomValue);

		TopDisplay.text = String.Format("{0,3}", topValue);
		BottomDisplay.text = String.Format("{0,3}", bottomValue);

		ModuleLog("Generated values {0}, {1}", topValue, bottomValue);

		// Calculate and print a possible solution
		ModuleLog("One possible solution:");
		ModuleLog("  ({0}|{1})", topValue, bottomValue);

		var currentSymbols = new List<LegendreSymbol>();
		currentSymbols.Add(new LegendreSymbol(topValue, bottomValue));

		bool nextLineHasNegativeOne = false;

		while (currentSymbols.Count > 0)
		{
			// Process each Legendre symbol from the last line
			var tokensToPrint = new List<String>();
			var nextSymbols = new List<LegendreSymbol>();
			
			if (nextLineHasNegativeOne)
			{
				tokensToPrint.Add("-1");
			}

			foreach (var symbol in currentSymbols)
			{
				if (symbol.IsSquaredTop)
				{
					// Symbols written as (a²|p) instantly become 1
					tokensToPrint.Add("1");
				}
				else if (IsPerfectSquare(symbol.A))
				{
					// Make this a squared term right away
					var newSymbol = new SquaredTopLegendreSymbol(symbol.A, symbol.P);
					tokensToPrint.Add(newSymbol.ToString());
					nextSymbols.Add(newSymbol);
				}
				else if (symbol.A == 2)
				{
					// (2|p) equals 1 if p % 8 is 1 or 7, -1 otherwise
					int pMod8 = symbol.P % 8;
					int legendreResult = (pMod8 == 1 || pMod8 == 7) ? 1 : -1;
					ModuleLog("    {0} mod 8 = {1}, so (2|{0}) = {2}", symbol.P, pMod8, legendreResult);

					if (legendreResult == -1)
					{
						nextLineHasNegativeOne = !nextLineHasNegativeOne;
					}
					tokensToPrint.Add(legendreResult.ToString());
				}
				else if (symbol.A == -1)
				{
					// (-1|p) equals 1 if p % 4 == 1, -1 otherwise
					int pMod4 = symbol.P % 4;
					int legendreResult = (pMod4 == 1) ? 1 : -1;
					ModuleLog("    {0} mod 4 = {1}, so (-1|{0}) = {2}", symbol.P, pMod4, legendreResult);

					if (legendreResult == -1)
					{
						nextLineHasNegativeOne = !nextLineHasNegativeOne;
					}
					tokensToPrint.Add(legendreResult.ToString());
				}
				else if (symbol.A % symbol.P == symbol.P - 1)
				{
					// Convert to (-1|p)
					var newSymbol = new LegendreSymbol(-1, symbol.P);
					tokensToPrint.Add(newSymbol.ToString());
					nextSymbols.Add(newSymbol);
				}
				else if (symbol.A > symbol.P)
				{
					// Reduce so a < p
					var newSymbol = new LegendreSymbol(symbol.A % symbol.P, symbol.P);
					tokensToPrint.Add(newSymbol.ToString());
					nextSymbols.Add(newSymbol);
				}
				else if (IsPrime(symbol.A))
				{
					// Quadratic reciprocity
					bool flip = symbol.A % 4 == 3 && symbol.P % 4 == 3;
					if (flip)
					{
						nextLineHasNegativeOne = !nextLineHasNegativeOne;
					}

					var newSymbol = new LegendreSymbol(symbol.P, symbol.A);
					tokensToPrint.Add((flip ? "-" : "") + newSymbol.ToString());
					nextSymbols.Add(newSymbol);
				}
				else
				{
					// Top value is composite
					int squaredTerm = 1;
					var squareFreePrimes = new List<int>();

					foreach (var entry in PrimeFactorization(symbol.A))
					{
						int p = entry.Key;
						int exp = entry.Value;

						if (exp >= 2)
						{
							squaredTerm *= (int) Math.Pow(p, exp - exp % 2);
						}
						if (exp % 2 == 1)
						{
							squareFreePrimes.Add(p);
						}
					}

					if (squaredTerm != 1)
					{
						var newSymbol = new SquaredTopLegendreSymbol(squaredTerm, symbol.P);
						tokensToPrint.Add(newSymbol.ToString());
						nextSymbols.Add(newSymbol);
					}

					foreach (int p in squareFreePrimes)
					{
						var newSymbol = new LegendreSymbol(p, symbol.P);
						tokensToPrint.Add(newSymbol.ToString());
						nextSymbols.Add(newSymbol);
					}
				}
			}

			ModuleLog("= " + String.Join(" × ", tokensToPrint.ToArray()));

			currentSymbols = nextSymbols;

			if (currentSymbols.Count == 0 && tokensToPrint.Count > 1)
			{
				// One final multiply to get a single answer
				ModuleLog("= {0}", nextLineHasNegativeOne ? "-1" : "1");
			}
		}

		ExpectedAnswer = !nextLineHasNegativeOne;

		if (ExpectedAnswer)
		{
			ModuleLog("{0} IS a quadratic residue modulo {1}.  Expected press: \"R\"", topValue, bottomValue);
		}
		else
		{
			ModuleLog("{0} is NOT a quadratic residue modulo {1}.  Expected press: \"N\"", topValue, bottomValue);
		}

		WillAcceptAnswer = true;
	}

	IEnumerator PerformStrike()
	{
		BombModule.HandleStrike();
		TopDisplay.text = "---";
		BottomDisplay.text = "---";
		WillAcceptAnswer = false;
		yield return new WaitForSeconds(0.75f);
		GenerateValues();
	}

	#pragma warning disable 414
	string TwitchHelpMessage = "Use \"!{0} press r\", \"!{0} press n\", \"!{0} r\", or \"!{0} n\" to press a button.";
	#pragma warning restore 414

	KMSelectable[] ProcessTwitchCommand(string command)
	{
		if (command.Equals("press r", StringComparison.InvariantCultureIgnoreCase) || command.Equals("r", StringComparison.InvariantCultureIgnoreCase))
		{
			return new KMSelectable[] { YesButton };
		}
		else if (command.Equals("press n", StringComparison.InvariantCultureIgnoreCase) || command.Equals("n", StringComparison.InvariantCultureIgnoreCase))
		{
			return new KMSelectable[] { NoButton };
		}
		else
		{
			return null;
		}
	}
}
