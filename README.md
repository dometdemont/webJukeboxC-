# webJukebox
This Razor application exposes a simple web site offering MIDI features:
## Playback
A catalog of MIDI files is offered for playback ; associated to each MIDI file in the catalog are:
- a description of the performance,
- the name of the performer,
- some timing information: delay before the first note, performance duration, delay after the last note,
- an URL to display during the playback, typically documentting the performance,
- the relative speed of the playback.  

During playback, the user is offered to:
- stop the playback
- pause/resume
- refresh the display

## Recording
If a MIDI input device is detected, the user is offered a Recording feature. 
Once Recording is started, all MIDI events are recorded, some basic informations being displayed like the first note timing, the number of notes, the last note timing, etc.

The Recording ends when the user hits the Stop link on the web page: the MIDI data is then saved locally in the catalog directory with a name based on the time and date.
This new recording is made available as the first entry in the catalog, for immediate playback.
Subsequent recordings are saved the same way in a time stamped file, only the latter being exposed in the catalog; previous
recording files are still avaiable in the catalog directory, not exposed in the Welcome page for replay but available for further onboarding in the compiled catalog.

## Forwarding
If a MIDI input device is detected, the user is offered a Forwarding feature. 
The value of this forwarding feature is tightly designed for connecting a Korg keyboard or a Yamaha EZ AG guitar to a Classic Organ Works C722 pipe organ, 
by performind ad-hoc adaptation to allow playing the pipe organ remotey from such MIDI devices. For instance, the piano octave below the first actual organ one is 
dedicated to driving the swell boxes. More details in the Forward implementation.

The Forwarding ends when the user hits the Stop link on the web page.

# Implementation
This application is based on the outstanding Melanchall.DryWetMidi C# library.

It is implemented as two main files:
- Title.cs: the Title class actually implementing the playback/record/forward features on top of this library
- Index.cshtml.cs: the main class in charge of allocating the catalog of titles and dynamically building the html pages.
