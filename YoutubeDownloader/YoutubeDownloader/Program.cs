using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace YoutubeDownloader
{
	class Program
	{
		struct VideoInfo
		{
			public string Title;
			public string Href;
			public string Views;
			public string Length;
		}

		static void Main(string[] args)
		{
#if DEBUG
			bool downloadDirect = false;
			string searchString = "blackpink".Replace(' ', '+');
			string websiteUrl = "https://www.youtube.com/results?search_query=" + searchString;
#else
			string searchString;
			string websiteUrl;

			bool downloadDirect = false;

			try
			{
				if(args[0].Contains("http://") || args[0].Contains("https://"))
				{
					websiteUrl = args[0];
					downloadDirect = true;
				}
				else
				{
					searchString = args[0].Replace(' ', '+');
					websiteUrl = "https://www.youtube.com/results?search_query=" + searchString;
				}

			}
			catch (Exception)
			{
				Console.WriteLine("Please enter search string or URL");
				return;
			}		
#endif

			if(downloadDirect)
			{
				DownloadVideoAsMp3(websiteUrl);
			}
			else
			{
				List<VideoInfo> videos = ScrapeSearchResults(websiteUrl);
				PrintVideosToConsole(videos);
				int RequestedDownloadId = AskRequestedVideoNumber();
				DownloadVideoAsMp3(videos[RequestedDownloadId]);
			}
		}

		static void PrintVideosToConsole(List<VideoInfo> videos)
		{
			for (int i = 0; i < videos.Count; i++)
			{
				VideoInfo info = videos[i];

				Console.WriteLine("----------------------{" + i + "}----------------------");
				Console.WriteLine("Title:\t" + info.Title);
				Console.WriteLine("URL: " + info.Href + " | Views: " + info.Views + " | Length: " + info.Length);
			}
		}

		static int AskRequestedVideoNumber()
		{
			Console.Write("\nWhich one would you like to download?\nNumber: ");
			string answer = Console.ReadLine();

			int numberAnswered = Int32.Parse(answer);
			return numberAnswered;
		}

		static List<VideoInfo> ScrapeSearchResults(string searchQueryUrl)
		{
			HttpClient client = new HttpClient();
			string rawText = client.GetStringAsync(searchQueryUrl).Result;

			HtmlDocument document = new HtmlDocument();
			document.LoadHtml(rawText);
			HtmlNodeCollection collection = document.DocumentNode.SelectNodes("//div[@class='yt-lockup-content']");

			List<VideoInfo> videos = new List<VideoInfo>();

			foreach (HtmlNode node in collection)
			{
				//First check if playlist, if it is, ignore this entry
				if (node.Element("h3").Element("span").InnerHtml.Contains("Afspeellijst") ||
					node.Element("h3").Element("span").InnerHtml.Contains("Kanaal"))
				{
					continue;
				}

				//Create new video info for each found video
				VideoInfo videoInfo = new VideoInfo();

				HtmlNode titleNode = node.Element("h3").Element("a");

				//Get string and href
				videoInfo.Title = titleNode.GetAttributeValue("title", "No Title");
				videoInfo.Href = titleNode.GetAttributeValue("href", "No href");

				//Get video length
				HtmlNode spanWithVideoLength = node.Element("h3").Element("span");
				videoInfo.Length = spanWithVideoLength.InnerHtml.Split(new string[] { "Duur: " }, StringSplitOptions.None)[1];
				videoInfo.Length = videoInfo.Length.Substring(0, videoInfo.Length.Length - 1);

				//Get views
				HtmlNode divWithViews = node.Elements("div").ToList()[1];
				List<HtmlNode> LiNodesWithViews = divWithViews.Element("ul").Elements("li").ToList();
				foreach (HtmlNode liWithViewsNode in LiNodesWithViews)
				{
					string textInThisLi = liWithViewsNode.InnerHtml;
					if (textInThisLi.Contains("weergaven"))
					{
						videoInfo.Views = textInThisLi.Split(' ')[0];
						break;
					}
				}

				videos.Add(videoInfo);
			}
			return videos;
		}

		static void DownloadVideoAsMp3(string url)
		{
			Process process = new Process();
			process.StartInfo.FileName = "youtube-dl.exe";
			process.StartInfo.Arguments = "--extract-audio --audio-format mp3 --output \"%(title)s.%(ext)s\" " + url;
			process.StartInfo.CreateNoWindow = true;
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.RedirectStandardOutput = true;
			process.Start();

			Console.WriteLine("");
			do
			{
				Thread.Sleep(1);
				Console.WriteLine(process.StandardOutput.ReadLine());
			}
			while (!process.HasExited);

			Console.WriteLine("Download & Conversion Complete!");
			Console.WriteLine("File saved as Mp3");
		}

		static void DownloadVideoAsMp3(VideoInfo videoInfo)
		{
			Process process = new Process();
			process.StartInfo.FileName = "youtube-dl.exe";
			process.StartInfo.Arguments = "--extract-audio --audio-format mp3 --output \"%(title)s.%(ext)s\" https://youtube.com" + videoInfo.Href;
			process.StartInfo.CreateNoWindow = true;
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.RedirectStandardOutput = true;
			process.Start();

			Console.WriteLine("");
			do
			{
				Thread.Sleep(1);
				Console.WriteLine(process.StandardOutput.ReadLine());
			}
			while (!process.HasExited);

			Console.WriteLine("Download & Conversion Complete!");
			Console.WriteLine("File saved as " + videoInfo.Title + ".mp3");
		}
	}
}
