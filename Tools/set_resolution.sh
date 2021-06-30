displays=$(DISPLAY=:0 xrandr | grep -e " connected [^(]" | sed -e "s/\([A-Z0-9]\+\) connected.*/\1/")
resolution="1920x1080"

for display in $displays
do
    DISPLAY=:0 xrandr -d :0 --output $display --mode $resolution
done
