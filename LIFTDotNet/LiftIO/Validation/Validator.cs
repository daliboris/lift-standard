using System;
using System.Xml;
using Commons.Xml.Relaxng;
using LiftIO.Parsing;

namespace LiftIO.Validation
{
	/// <summary>Provide progress reporting for validation.</summary>
	/// <remarks>TODO: provide a single IProgressReport interface for LiftIO.</remarks>
	public interface IValidationProgress
	{
		string Status { set; get; }
		void Step(int n);
		int MaxRange { set; get; }
	}

	/// <summary>
	/// Trivial, nonfunctional implementation of IValidationProgress.
	/// </summary>
	/// <remarks>
	/// TODO: provide a single IProgressReport interface for LiftIO, and a single trivial
	/// implementation thereof.
	/// </remarks>
	public class NullValidationProgress : IValidationProgress
	{
		private string _status = String.Empty;
		private int _max = 20;
		private int _step = 0;

		public NullValidationProgress()
		{
		}

		#region IValidationProgress Members

		public string Status
		{
			get { return _status; }
			set { _status = value; }
		}

		public void Step(int n)
		{
			_step += n;
		}

		public int MaxRange
		{
			get { return _max; }
			set { _max = value; }
		}

		#endregion
	}

    public class Validator
    {
        static public string GetAnyValidationErrors(string path)
        {
			return GetAnyValidationErrors(path, null);
        }

		static public string GetAnyValidationErrors(string path, IValidationProgress progress)
		{
			using (XmlTextReader documentReader = new XmlTextReader(path))
			{
				return GetAnyValidationErrors(documentReader, progress);
			}
		}
		
		static public string GetAnyValidationErrors(XmlTextReader documentReader)
        {
			return GetAnyValidationErrors(documentReader, null);
        }

		static public string GetAnyValidationErrors(XmlTextReader documentReader,
			IValidationProgress progress)
		{
			RelaxngValidatingReader reader = new RelaxngValidatingReader(
				documentReader,
				new XmlTextReader(typeof(LiftMultiText).Assembly.GetManifestResourceStream("LiftIO.Validation.lift.rng")));
			reader.ReportDetails = true;
			string lastGuy = "lift";
			int line = 0;
			int step = 0;
			if (progress != null)
			{
				try
				{
					string sFilePath = documentReader.BaseURI.Replace("file://", "");
					if (sFilePath.StartsWith("/"))
					{
						// On Microsoft Windows, the BaseURI may be "file:///C:/foo/bar.lift"
						if (sFilePath.Length > 3 &&
							Char.IsLetter(sFilePath[1]) && sFilePath[2] == ':' && sFilePath[3] == '/')
						{
							sFilePath = sFilePath.Substring(1);
						}
					}
					System.IO.FileInfo fi = new System.IO.FileInfo(sFilePath);
					// Unfortunately, XmlTextReader gives access to only line numbers,
					// not actual file positions while reading.  A check of 8 Flex
					// generated LIFT files showed a range of 43.9 - 52.1 chars per
					// line.  The biatah sample distributed with WeSay has an average
					// of only 23.1 chars per line.  We'll compromise by guessing 33.
					// The alternative is to read the entire file to get the actual
					// line count.
					int maxline = (int)(fi.Length / 33);
					if (maxline < 8)
						progress.MaxRange = 8;
					else if (maxline < 100)
						progress.MaxRange = maxline;
					else
						progress.MaxRange = 100;
					step = (maxline + 99) / 100;
					if (step <= 0)
						step = 1;
				}
				catch
				{
					step = 100;
				}
			}
			try
			{
				while (!reader.EOF)
				{
					// Debug.WriteLine(reader.v
					reader.Read();
					lastGuy = reader.Name;
					if (progress != null && reader.LineNumber != line)
					{
						line = reader.LineNumber;
						if (line % step == 0)
							progress.Step(1);
					}
				}
			}
			catch (Exception e)
			{
				if (reader.Name == "version" && (lastGuy == "lift" || lastGuy == ""))
				{
					return String.Format(
						"This file claims to be version {0} of LIFT, but this version of the program uses version {1}",
						reader.Value, LiftVersion);
				}
				string m = string.Format("{0}\r\nError near: {1} {2} '{3}'", e.Message, lastGuy, reader.Name, reader.Value);
				return m;
			}
			return null;
		}

        public static string LiftVersion
        {
            get
            {
                return "0.13";
            }
        }

        public static void CheckLiftWithPossibleThrow(string pathToLiftFile)
        {
			CheckLiftWithPossibleThrow(pathToLiftFile, null);
        }

		public static void CheckLiftWithPossibleThrow(string pathToLiftFile, IValidationProgress progress)
		{
			string errors = GetAnyValidationErrors(pathToLiftFile, progress);
			if (!String.IsNullOrEmpty(errors))
			{
				errors = string.Format("The dictionary file at {0} does not conform to the current version of the LIFT format ({1}).  The RNG validator said: {2}.",
									   pathToLiftFile, LiftVersion, errors);
				throw new LiftFormatException(errors);
			}
		}

        public static string GetLiftVersion(string pathToLift)
        {
            string liftVersionOfRequestedFile = String.Empty;

            XmlReaderSettings readerSettings = new XmlReaderSettings();
            readerSettings.ValidationType = ValidationType.None;
            readerSettings.IgnoreComments = true;

            using (XmlReader reader = XmlReader.Create(pathToLift, readerSettings))
            {
                if (reader.IsStartElement("lift"))
                    liftVersionOfRequestedFile = reader.GetAttribute("version");
            }
            if (String.IsNullOrEmpty(liftVersionOfRequestedFile))
            {
                throw new LiftFormatException(String.Format("Cannot import {0} because this was not recognized as well-formed LIFT file (missing version).", pathToLift));
            }
            return liftVersionOfRequestedFile;
        }   
    }
}