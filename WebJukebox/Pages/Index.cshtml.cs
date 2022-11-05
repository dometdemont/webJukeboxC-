using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Linq;
using System.Threading;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.Common;
using System.Threading.Channels;
using WebJukebox.Pages.Shared;

namespace WebJukebox.Pages
{
    public class IndexModel : PageModel
    {
        // Message receives the dynamic html code
        public string Message { get; private set; } = "PageModel in C#";
        
        // Shortcuts for known performers
        private static readonly string d3m = "Dominique Domet de Mont";
        private static readonly string lml = "Louis-Marie Lardic";

        // Famous scores
        static Score BACH = new Score("https://orguestjo.go.yj.fr/TestJukebox/datas/medias/MP4/1100000 - BACHLISZT.mp4", 91, 0);

        // The static playlist
        private static Title[] playList = {
            // The first item is reserved for live recording: file and timing will be updated when record is complete.
            // Since the file name is using the recording date & time, subsequent recordings do not override previous ones, even if the playlist shows only the latest
            new Title("EMPTY.MID", "", "1,5,2", null, 100, null, null),
            
            // Other items are known and documented
            new Title("LISZT.MID", "Franz Liszt : Prélude et Fugue sur B.A.C.H. (10')", "15, 617, 20", lml, 100, "https://fr.wikipedia.org/wiki/Fantasie_und_Fuge_%C3%BCber_das_Thema_B-A-C-H#p-lang-btn", BACH),
            new Title("WAGNER.MID", "Richard Wagner : Mort de Isolde, transcription (8'40)", "8, 510, 2", lml, 100, "https://fr.wikipedia.org/wiki/Liebestod#p-lang-btn", null),
            new Title("DUPRE.MID", "Marcel Dupré : Prélude et Fugue en sol mineur (7')", "12, 403, 10", lml, 100, "https://fr.wikipedia.org/wiki/Marcel_Dupr%C3%A9#p-lang-btn", null),
            new Title("FRANCK.MID", "César Franck : Troisième Choral (10'20)", "13, 622, 20", lml, 100, "https://fr.wikipedia.org/wiki/C%C3%A9sar_Franck#p-lang-btn", null),

            new Title("MESSIAEN.MID", "Olivier Messiaen : Banquet céleste (6')", "5, 375, 19", d3m, 100, "https://en.wikipedia.org/wiki/Le_Banquet_C%C3%A9leste#p-lang-btn", null),
            new Title("ASCENSIO.MID", "Olivier Messiaen : Prière du Christ montant vers son Père (7')", "8, 425, 10", d3m, 100, "https://fr.wikipedia.org/wiki/Olivier_Messiaen#p-lang-btn", null),
            new Title("COUPERIN.MID", "François Couperin : Tierce en taille (4')", "6, 240, 2", d3m, 100, "https://fr.wikipedia.org/wiki/Fran%C3%A7ois_Couperin#p-lang-btn", null),
            new Title("OBOE.MID", "Alessandro Marcello / JS Bach : Concerto en Ré mineur (4'40)", "4, 276, 2", d3m, 100, "https://fr.wikipedia.org/wiki/Concerto_pour_hautbois_en_r%C3%A9_mineur_de_Marcello#p-lang-btn", null),
            new Title("CHROMORN.MID", "François Couperin : Chromorne en taille (4')", "5, 233, 2", d3m, 100, "https://fr.wikipedia.org/wiki/Fran%C3%A7ois_Couperin#p-lang-btn", null),
            new Title("BUXTEHUD.MID", "Dietrich Buxtehude : Passacaille en Ré mineur (7')", "5, 420, 5", d3m, 100, "https://fr.wikipedia.org/wiki/Passacaille_en_r%C3%A9_mineur_de_Buxtehude#p-lang-btn", null),
            new Title("LANGLAIS.MID", "Jean Langlais : Chant de Paix (2'30)", "4, 150, 4", d3m, 100, "https://fr.wikipedia.org/wiki/Jean_Langlais#p-lang-btn", null),
            new Title("GUILMANT.MID", "Alexandre Guilmant : Noël 'Or dites-nous Marie' (2'20)", "6, 130, 4", d3m, 100, "https://fr.wikipedia.org/wiki/Alexandre_Guilmant#p-lang-btn", null),
            new Title("DISPUTE.MID", "Yann Tiersen : La Dispute - imitation musette (2'20)", "4, 133, 2", d3m, 100, "https://fr.wikipedia.org/wiki/Le_Fabuleux_Destin_d%27Am%C3%A9lie_Poulain_(bande_originale)#p-lang-btn", null),
            new Title("HIMMEL.MID", "J.S. Bach : Nun schleuss den Himmel auf (2'10)", "5, 125, 2", d3m, 100, "https://fr.wikipedia.org/wiki/Orgelb%C3%BCchlein#p-lang-btn", null),
            new Title("TODESBAND.MID", "J.S. Bach : Christ lag in Todesbanden (1'30)", "2, 90, 2", d3m, 100, "https://fr.wikipedia.org/wiki/Orgelb%C3%BCchlein#p-lang-btn", null),
            new Title("BACH_846.mid", "J.S. Bach: Prélude en Do Majeur BWV 846 (2'25)", "4, 138, 2", d3m, 100, "https://fr.wikipedia.org/wiki/Pr%C3%A9lude_et_fugue_en_ut_majeur_(BWV_846)#p-lang-btn", null),
            new Title("TOCAREM.MID", "J.S. Bach : Toccata en Ré mineur (2'30)", "2, 145, 5", d3m, 100, "https://fr.wikipedia.org/wiki/Toccata_et_fugue_en_r%C3%A9_mineur#p-lang-btn", null),
            new Title("CHACONNE.MID", "Johann Pachelbel : Chaconne en fa mineur (10')", "5, 607, 2", d3m, 100, "https://fr.wikipedia.org/wiki/Chaconne_en_fa_mineur_(Pachelbel)#p-lang-btn", null),
            new Title("CANTABIL.MID", "Charles-Marie Widor : Andante cantabile de la 3ème symphonie (5'30)", "5, 330, 2", d3m, 100, "https://fr.wikipedia.org/wiki/Charles-Marie_Widor#p-lang-btn", null),
            new Title("BOELMANN.mid", "Léon Boëllmann : Toccata de la suite gothique (3'45)", "4, 223, 2", d3m, 120, "https://en.wikipedia.org/wiki/Suite_Gothique#p-lang-btn", null),
            new Title("Reset19.mid", "Série de combinaison n° 19 (pas de notes)", "1,2,1", null, 100, null, null)
        };
        
        // MIDI files location on the server, also used to store new recordings
        private static string CatalogPath="C:/Users/domin/Desktop/Jukebox/";

        // List of ids accepted on GET requests: 
        // - 0..N-1: the title to play
        // - N... commands stop, pause, resume, record
        private static int stopId = playList.Length;
        private static int pauseId = stopId+1;
        private static int resumeId = pauseId+1;
        private static int recordId = resumeId + 1;
        private static int forwardId = recordId + 1;

        private static Title? currentTitle =null;

        private readonly ILogger<IndexModel> _logger;
        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        // Build the welcome page: 
        // Display the last error if any, the list of titles available for playing, the MIDI devices in/out
        // Three actions: Refresh Forward Record
        private void Welcome()
        {
            Message = "";
            string err = Title.getLastError();
            if (err != null) Message += "<p>Error: " + err + "</p>";
            currentTitle = null;

            if (OutputDevice.GetDevicesCount() > 0)
            {
                OutputDevice lastMidiDevice = OutputDevice.GetByIndex(OutputDevice.GetDevicesCount() - 1);
                Title.SetMidiDevice(lastMidiDevice);
                InputDevice lastMidiInputDevice=null;
                if (InputDevice.GetDevicesCount() > 0)
                {
                    lastMidiInputDevice = InputDevice.GetByIndex(InputDevice.GetDevicesCount() - 1);
                    Title.SetMidiInputDevice(lastMidiInputDevice);
                }
                Title.SetCatalogPath(CatalogPath);
                Message += "<ul><li>Périphérique de sortie MIDI : " + lastMidiDevice.Name+ "</li>";
                if(lastMidiInputDevice != null) Message += "<li>Périphérique d'enregistrement MIDI : " + lastMidiInputDevice.Name + "</li>";
                Message += "<li>Répertoire du catalogue : " + CatalogPath + "</li></ul>";
                Message += "<table><tr><td align=left>";
                if (lastMidiInputDevice != null)
                {
                    if(Title.IsFree())Message += "<a href=" 
                            + recordId + " title='Sortie séquenceur MIDI orgue => entrée MIDI jukebox'> Enregistrer</a></td><td><a href=" 
                            + forwardId + " title='Sortie MIDI du clavier Korg ou de la guitare Yamaha EZ-AG => entrée MIDI jukebox'> Korg/Yamaha</a>";
                }
                Message += "</td><td align=right><a id='refresh' href='/'> Actualiser</a></tr></table>";
                Message += EnableAutoRefresh(20);

                Message += "<h3>Liste des pièces disponibles</h3><ul>";
                for (int i = 0; i < playList.Length; i++)
                {
                    Message += "<li><a href=" + i + " title='Interprétation : " + playList[i].performer + "'>" + playList[i].description  +"</a></li>";
                };
                Message += "</ul>";
            }
            else
            {
                Message = "<h3>Aucun périphérique de sortie MIDI détecté/h3>";
            }
            Message += "<h6>&copy 2022 Orgue et Musique Sacrée à la Basilique St Joseph de Grenoble</h6>";
            
        }
        // Builds a javascript fragment enabling auto refresh at a period second rate 
        // Requires a clickable HTML element with id refresh
        private String EnableAutoRefresh(int period)
        {
            return @"
<script>
var refreshLink = document.getElementById(""refresh"")
function doRefresh(){
  refreshLink.click();
}
setTimeout(doRefresh, "+(1000*period).ToString()+@")
</script>";        
        }
        // Build the Record page
        // Display the recording status
        // Two actions: Refresh and Stop
        // Auto refreshed at 10s rate
        private void Record()
        {
            string recordingState = Title.getRecordingInfo();
            Message = "<h3>Enregistrement en cours</h3><p>" + recordingState + "</p>";
            Message += "<table><tr>";
            Message += "<td align=left><a href=" + stopId + ">Arrêter</a></td>";
            Message += "<td align=right><a id='refresh' href='/'> Actualiser</a></td>";
            Message += "</tr></table>";
            Message += EnableAutoRefresh(10);
        }
        
        // Build the Forward page
        // Display the recording status
        // Two actions: Refresh and Stop
        // Auto refreshed at 3s rate
        private void Forward()
        {
            string forwardingState = Title.getForwardedInfo();
            Message = "<h3>Instrument MIDI en cours</h3>" + forwardingState;
            Message += "<table><tr>";
            Message += "<td align=left><a href=" + stopId + ">Arrêter</a></td>";
            Message += "<td align=right><a id='refresh' href='/'> Actualiser</a></td>";
            Message += "</tr></table>";
            Message += EnableAutoRefresh(3);
        }

        // Build the Current page
        // Display the current play, with an optional count down (typically while playing, not paused)
        // Three actions: Stop/Pause-Resume/Refresh
        private void Current(bool showCountDown)
        {
            if(currentTitle == null) { Welcome(); return; }

            Message = "<h3>Pièce en cours d'audition</h3><p>" + currentTitle.description + "</p>";
            string err = Title.getLastError();
            if (err != null) Message += "<p>Error: " + err + "</p>";
            if (currentTitle.performer != null)
            {
                Message += "<h5>Interprétation : "+currentTitle.performer+"</h5>";
            }
            Message += "<table><tr>";
            Message += "<td align=left><a href=" + stopId + ">Arrêter</a></td>";
            if (Title.IsPlaying())
            {
                Message += "<td align=center><a href=" + pauseId + ">Pause</a></td>";
            }
            else
            {
                Message += "<td align=center><a href=" + resumeId + ">Reprendre</a></td>";
            }

            Message += "<td align=right><a id='refresh' href='/'> Actualiser</a></td>";
            Message += "</tr></table>";
            if (currentTitle.score != null)
            {
                Message += @"<video width=100% id=""score""><source src=""" + currentTitle.score.video + @""" ></video>";
            }
            CountDown(showCountDown);
            if (currentTitle.doc != null)
            {
                Message += @"<iframe width=100% height=400px src=""" + currentTitle.doc+@""" title=""Wikipedia""></iframe> ";
            }
        }
        
        // The count down display 
        // This is a javascript fragment receiving dynamically a 'distances' variable providing the play timing as a table of 4 durations expressed in seconds:
        // - load: delay before the first note is played
        // - perform: the actual play duration
        // - unload: delay after the last note is played
        // - current: the current position in the play
        // The javascript countDown function will refresh the HTML variable with id countDown at 'rate' period with the current phase and the remaining time in this phase
        private void CountDown(Boolean run) 
        {
            int scoreSpeed = 100;
            int scoreOffset = 0;
            // if a score is attached to the current title
            if (currentTitle.score != null)
            {
                scoreSpeed = currentTitle.score.speed;
                scoreOffset = currentTitle.score.offset;
            }
            Message += @"<p id=""countDown""></p>
<script>
var distances=[" + currentTitle.timing + ","+Title.GetElapsed(currentTitle.speed)+ @"]
var run="+run.ToString().ToLower()+ @"
var scoreSpeed="+ scoreSpeed + @"
var scoreOffset=" + scoreOffset + @"
var phases=[""Chargement : "", ""Exécution : "", ""Déchargement : ""]
// Update the count down every 1 second
var rate=1000;
var phase=0;
var elapsed = distances[phases.length]
var scoreCurrentTime = elapsed
phases.forEach(function(p, i){
  if(elapsed <= distances[i]){
    distances[i]-=elapsed
    elapsed=distances[phases.length]=0
  }else{
    phase=i
    elapsed-=distances[i]
    distances[i]=0
  }
})
var distance = distances[phase]
var refreshLink = document.getElementById(""refresh"")
var score = document.getElementById (""score"");
function doRefresh(){
  refreshLink.click();
}
function countDown() {
  var minutes = Math.floor((distance % (60 * 60)) / 60);
  var seconds = Math.floor(distance % 60);
  document.getElementById(""countDown"").innerHTML = phases[phase] + (minutes?minutes+""mn "":"""")+seconds+""s""
  if(distance == 0 && phase < phases.length-1){
   phase++
   distance = distances[phase]
  }
  if(distance > 0)setTimeout(countDown, rate);
  distance--
}

if(run){
    if(rate)setTimeout(countDown, rate);
    setTimeout(doRefresh, 1000*(distances[0]+distances[1]+distances[2]-distances[3]+4))
}

if(score){
    score.currentTime=(scoreCurrentTime-scoreOffset)*scoreSpeed/100
    score.playbackRate = scoreSpeed/100
    if(run){
        score.play(); 
    }else{
        score.pause();
    }
}
</script>
";
        }

        // The GET request processing
        // List of ids accepted: 
        // - 0..N-1: the title to play
        // - N... commands stop, pause, resume, record
        // Store the new recording in the first element of the playlist (while not erasing previous recorded files)

        public void OnGet(int? id)
        {
            if (id != null)
            {
                if (id < playList.Length)
                {
                    if (Title.IsFree())
                    {
                        currentTitle = playList[(int)id];
                        _logger.LogInformation("Starting playing id: {int}: {string}", id, currentTitle.description);
                        currentTitle.Start();
                        Current(true);
                    }
                    else
                    {
                        Current(Title.IsPlaying());
                    }
                    
                } else if(id == stopId) {
                    _logger.LogInformation("Stopping id: {int}", id);
                    if(currentTitle != null)currentTitle.Cancel();
                    Welcome();
                }
                else if (id == pauseId)
                {
                    _logger.LogInformation("Pausing id: {int}", id);
                    if (currentTitle != null)currentTitle.Pause();
                    Current(false);
                }
                else if (id == resumeId)
                {
                    _logger.LogInformation("Resuming id: {int}", id);
                    if (currentTitle != null)currentTitle.Resume();
                    Current(true);
                }
                else if(id == recordId)
                {
                    if (Title.IsFree())
                    {
                        _logger.LogInformation("Starting new recording");
                        // Store the new recording in the first element of the playlist
                        playList[0] = currentTitle = Title.Record();
                        Record();
                    }
                    else if (Title.IsRecording())
                    {
                        _logger.LogInformation("Displaying current recording");
                        Record();
                    }
                    else if (Title.IsPlaying())
                    {
                        _logger.LogInformation("Displaying current play");
                        Current(Title.IsPlaying());
                    }
                    else Welcome();
                }
                else if (id == forwardId)
                {
                    if (Title.IsFree())
                    {
                        _logger.LogInformation("Starting forwarding");
                        currentTitle = Title.Forward();
                        Forward();
                    }
                    else if (Title.IsForwarding())
                    {
                        _logger.LogInformation("Displaying current forwarding");
                        Forward();
                    }
                    else if (Title.IsPlaying())
                    {
                        _logger.LogInformation("Displaying current play");
                        Current(Title.IsPlaying());
                    }
                    else Welcome();
                }
            }
            else
            {
                if (Title.IsFree())
                {
                    _logger.LogInformation("Displaying Welcome");
                    Welcome();
                }else if (Title.IsRecording())
                {
                    _logger.LogInformation("Displaying current record");
                    Record();
                }
                else if (Title.IsForwarding())
                {
                    _logger.LogInformation("Displaying current forward");
                    Forward();
                }
                else
                {
                    _logger.LogInformation("Displaying current play");
                    Current(Title.IsPlaying());
                }
            }
        }
    }

    
 }
