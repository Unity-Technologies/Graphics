from . import win, osx_openglcore, osx_metal, linux, android, iphone, internal

cmd_map = {
    'win' : {
        'editmode': win.cmd_editmode,
        'playmode': win.cmd_playmode,
        'standalone' : win.cmd_standalone,
        'standalone_build' : win.cmd_standalone_build
    },
    'osx_openglcore' :  {
        'editmode': osx_openglcore.cmd_editmode,
        'playmode': osx_openglcore.cmd_playmode,
        'standalone' : osx_openglcore.cmd_standalone,
        'standalone_build' : osx_openglcore.cmd_standalone_build
    },
    'osx_metal' :  {
        'editmode': osx_metal.cmd_editmode,
        'playmode': osx_metal.cmd_playmode,
        'standalone' : osx_metal.cmd_standalone,
        'standalone_build' : osx_metal.cmd_standalone_build
    },
    'linux' : {
        'editmode': linux.cmd_editmode,
        'playmode': linux.cmd_playmode,
        'standalone' : linux.cmd_standalone,
        'standalone_build' : linux.cmd_standalone_build
    },
    'android' : {
        'editmode': android.cmd_editmode,
        'playmode': android.cmd_playmode,
        'standalone' : android.cmd_standalone,
        'standalone_build' : android.cmd_standalone_build
    },
    'iphone' : {
        'editmode': iphone.cmd_editmode,
        'playmode': iphone.cmd_playmode,
        'standalone' : iphone.cmd_standalone,
        'standalone_build' : iphone.cmd_standalone_build
    },
    'internal' : {
        'editmode': internal.cmd_editmode,
        'playmode': internal.cmd_playmode,
        'standalone' : internal.cmd_standalone,
        'standalone_build' : internal.cmd_standalone_build
    }  
}


def get_cmd(platform_name, api, test_platform_type, key):
    if key != "":
        return cmd_map.get(key)[test_platform_type]
    else:
        # Returns commands from platformname_apiname key if such key is present, or from platformname otherwise 
        return cmd_map.get(f'{platform_name}_{api["name"]}'.lower(), cmd_map.get(platform_name.lower()))[test_platform_type.lower()]

