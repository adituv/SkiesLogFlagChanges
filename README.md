# SkiesLogFlagChanges

This tool parses dolphin.log while the game is running to provide a concise log of
changes to progress flags and bits in Skies of Arcadia Legends.

To use it, you need to configure dolphin's log to write to file, and output
Memory Interface (MI) lines.  In addition, you need to set breakpoints over the
regions with flags and bits so they are output to the log.

Unfortunately, the program is currently too slow to parse all the data in real-time
when writes happen every frame or multiple times per frame.  Because of this, you
need to set several smaller regions avoiding some of the ones being set more often.

The regions I'm currently watching are:
```
80310a1c -> 80310a69
80310a72 -> 80310bc3
80310bc8 -> 80310c9f
80310ca4 -> 80311593
803115a8 -> 80311893
```

This avoids the eight flags used for tracking fish caught and value of fish caught,
and some unknown bits that are written every frame.  There are probably other
regions that need to be excluded that aren't yet known.
