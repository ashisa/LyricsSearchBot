using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace MusicMatchBot
{
    [BotAuthentication]
    [LuisModel("<LUIS APP ID>", "<LUIS API KEY>")]
    [Serializable]
    public class MusicMatch: LuisDialog<object>
    {
        [LuisIntent("")]
        public async Task None(IDialogContext context, LuisResult result)
        {
            string message = "I'm sorry I didn't understand. Try asking lyrics.";
            await context.PostAsync(message);
            context.Wait(MessageReceived);
        }

        [LuisIntent("GetLyrics")]
        public async Task ListInventory(IDialogContext context, LuisResult result)
        {
            await context.PostAsync("Looking for the lyrics for you...");

            string message = "";
            if (result.Entities.Count != 0)
            {
                EntityRecommendation ArtistEntity = new EntityRecommendation();
                EntityRecommendation TrackTitleEntity = new EntityRecommendation();
                var test1 = result.TryFindEntity("Artist", out ArtistEntity);
                var test2 = result.TryFindEntity("TrackTitle", out TrackTitleEntity);

                string artist = "", keyword = "";
                if (test1) artist = ArtistEntity.Entity;
                if (test2) keyword = TrackTitleEntity.Entity;

                message = await GetLyrics(keyword, artist);
                await context.PostAsync(message);
            }
            else
            {
                await context.PostAsync("Sorry - couldn't find any lyrics for you");
            }

            context.Wait(MessageReceived);
        }

        public async Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceived);
        }

        private async Task<string> GetLyrics(string keyword, string artist)
        {
            string lyricsText = $"Sorry - couldn't find suitable lyrics for {keyword}  by {artist}";
            int lyricID = await GetLyricsID(keyword, artist);
            if (lyricID > 0)
            {
                using (var client = new HttpClient())
                {
                    string uri = "http://api.musixmatch.com/ws/1.1/track.lyrics.get?track_id=" + lyricID + "&apikey=<musixmatch api key>";
                    HttpResponseMessage msg = await client.GetAsync(uri);

                    if (msg.IsSuccessStatusCode)
                    {
                        var jsonResponse = await msg.Content.ReadAsStringAsync();
                        if (!jsonResponse.Contains("\"status_code\":404"))
                        {
                            var _Data = JsonConvert.DeserializeObject<LyricsRootObject>(jsonResponse);

                            if (_Data.message.header.status_code != 404) lyricsText = _Data.message.body.lyrics.lyrics_body;
                        }
                    }
                }
            }

            return lyricsText;
        }

        private async Task<int> GetLyricsID(string keyword, string artist)
        {
            int lyricsID = 0;
            using (var client = new HttpClient())
            {
                string uri = "http://api.musixmatch.com/ws/1.1/track.search?q_track=" + keyword + "&q_artist=" + artist + "&f_has_lyrics=1&apikey=a450c46c69a3812a2292a14c6b2bf3ea";
                HttpResponseMessage msg = await client.GetAsync(uri);

                if (msg.IsSuccessStatusCode)
                {
                    var jsonResponse = await msg.Content.ReadAsStringAsync();
                    var _Data = JsonConvert.DeserializeObject<TrackRootObject>(jsonResponse);



                    if (_Data.message.body.track_list[0].track.lyrics_id != 0) lyricsID = _Data.message.body.track_list[0].track.lyrics_id;
                }
            }

            return lyricsID;
        }
    }

    //Track
    public class TrackHeader
    {
        public int status_code { get; set; }
        public double execute_time { get; set; }
        public int available { get; set; }
    }

    public class PrimaryGenres
    {
        public List<object> music_genre_list { get; set; }
    }

    public class SecondaryGenres
    {
        public List<object> music_genre_list { get; set; }
    }

    public class Track
    {
        public int track_id { get; set; }
        public string track_mbid { get; set; }
        public string track_spotify_id { get; set; }
        public int track_soundcloud_id { get; set; }
        public string track_name { get; set; }
        public int track_rating { get; set; }
        public int track_length { get; set; }
        public int commontrack_id { get; set; }
        public int instrumental { get; set; }
        public int @explicit { get; set; }
        public int has_lyrics { get; set; }
        public int has_subtitles { get; set; }
        public int num_favourite { get; set; }
        public int lyrics_id { get; set; }
        public int subtitle_id { get; set; }
        public int album_id { get; set; }
        public string album_name { get; set; }
        public int artist_id { get; set; }
        public string artist_mbid { get; set; }
        public string artist_name { get; set; }
        public string album_coverart_100x100 { get; set; }
        public string album_coverart_350x350 { get; set; }
        public string album_coverart_500x500 { get; set; }
        public string album_coverart_800x800 { get; set; }
        public string track_share_url { get; set; }
        public string track_edit_url { get; set; }
        public string updated_time { get; set; }
        public PrimaryGenres primary_genres { get; set; }
        public SecondaryGenres secondary_genres { get; set; }
    }

    public class TrackList
    {
        public Track track { get; set; }
    }

    public class TrackBody
    {
        public List<TrackList> track_list { get; set; }
    }

    public class TrackMessage
    {
        public TrackHeader header { get; set; }
        public TrackBody body { get; set; }
    }

    public class TrackRootObject
    {
        public TrackMessage message { get; set; }
    }

    //Lyrics
    public class LyricsHeader
    {
        public int status_code { get; set; }
        public double execute_time { get; set; }
    }

    public class Lyrics
    {
        public int lyrics_id { get; set; }
        public int can_edit { get; set; }
        public int locked { get; set; }
        public string action_requested { get; set; }
        public int verified { get; set; }
        public int restricted { get; set; }
        public int instrumental { get; set; }
        public int @explicit { get; set; }
        public string lyrics_body { get; set; }
        public string lyrics_language { get; set; }
        public string lyrics_language_description { get; set; }
        public string script_tracking_url { get; set; }
        public string pixel_tracking_url { get; set; }
        public string html_tracking_url { get; set; }
        public string lyrics_copyright { get; set; }
        public List<object> writer_list { get; set; }
        public List<object> publisher_list { get; set; }
        public string updated_time { get; set; }
    }

    public class LyricsBody
    {
        public Lyrics lyrics { get; set; }
    }

    public class LyricsMessage
    {
        public LyricsHeader header { get; set; }
        public LyricsBody body { get; set; }
    }

    public class LyricsRootObject
    {
        public LyricsMessage message { get; set; }
    }

}