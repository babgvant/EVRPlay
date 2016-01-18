--------------------------------------------------------------------------------
EVRPlay - A simple media player which plays using the Enhanced Video Renderer
Copyright (C) 2008 andy vt
http://babvant.com
--------------------------------------------------------------------------------

EVRPlay is a modifcation of the PlayWnd sample included in DirectShowLib.  
It is a simple media player application with a minimal user interface that uses the EVR Renderer for playback.

Some features include:

- commerical skipping if an edl is found
- MCE Remote integration
- two configurable skip intervals fwd and rev
- full screen mode
- clickable progress bar
- file bookmarking
- ext based filter preferences
- ext based filter blocking

Commands (items in "" are on the remote):

- double click, "0": toggle full screen
- F, "Skip Forward": skip forward 1 (60 sec)
- R, "Skip Back": skip back 1 (30 sec)
- G, "FFWD": skip forward 2 (30 sec)
- T, "RWD": skip back 2 (10 sec)
- P, "Play, Pause": toogle pause/play
- S, "Stop": stop playback
- M: toogle mute
- C, "2": toggle commercial skipping
- Left: skip back commercial span
- Right: skip forward commercial span
- A, "9": toggle apect ratio mode
- U, "7": toggle subtitles in video_ts mode
- Z: toogle zoom

1.0.0.1

- added basic pls support (Sage Webserver)
- added settings form
- added replace paths functionality
- added video_ts support

1.0.0.2

- added file history
- fixed interface cleanup issue

1.0.0.3

- added single instance mode
- overscan setting
- builtin sage webserver browsing
- Enter no longer toggles full screen mode, use 0 instead

1.0.0.4

- disable screen saver during playback
- fixed some full screen mode issues
- fixed an issue when selecting "Maximize"
- reworked status bar and web browser display
- added zoom
- hanndle "Watch (Streamed)" and "Watch (Local File)"
- stop dialog
- actions dialog

1.0.0.5

- fixed a keyhandling issue when the progress bar was displayed
- "info" button triggers actions dialog

1.0.0.6

- listens for some WM_USER+234 messages at "EVRPlayApp" "EVRPlayWin"
- added Media Browser mode
- added DTB XML option
- Recorded TV & My Videos buttons launch respective tabs in Media Browser
- added PriorityClass setting

1.0.0.7

- Bug fixes for DVD/VIDEO_TS playback
- Bug fixes for MediaBrowser mode
- Open URL option
- Add DVD Menu option to Info dialog
- Add Close player option to Stop dialog
- Write last run path to SOFTWARE\babgvant\EVRPlay, LastRunFrom
- Fixed an issue if a filter that do not have a name is enumerated
- Added DtbXmlPath setting

1.0.0.8

- Added Multi-channel WMA output support (only useful if not using the WMAPro over S/PDIF DMO), if not passing PCM over HDMI should enable the use of AC3Filter to encode to AC3.
- Fixed DVD bookmarking issue
- Fixed DVD menu navigation issue
- Control buttons work with DVDs now