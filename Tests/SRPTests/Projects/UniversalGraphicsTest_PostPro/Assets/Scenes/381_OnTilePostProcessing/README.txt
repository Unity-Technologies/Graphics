The Bloom volume override is explictly added as a safety.
When somehow the wrong renderer would be used, one that
uses the regular post processing, instead of the on-tile
post processing, bloom will show up in the ref image and
fail the test. We assume here bloom has no effect in the
On-Tile PP. Once that would be true, then this might need
to be configured differently.
