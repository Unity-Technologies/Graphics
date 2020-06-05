from . import win, osx_openglcore, osx_metal, linux, android, osx_metal_split, linux_split, iphone

cmd_map = {
    'win' : {
        'not_standalone': win.cmd_not_standalone,
        'standalone' : win.cmd_standalone,
        'standalone_build' : win.cmd_standalone_build
    },
    'osx_openglcore' :  {
        'not_standalone': osx_openglcore.cmd_not_standalone,
        'standalone' : osx_openglcore.cmd_standalone,
        'standalone_build' : osx_openglcore.cmd_standalone_build
    },
    'osx_metal' :  {
        'not_standalone': osx_metal.cmd_not_standalone,
        'standalone' : osx_metal.cmd_standalone,
        'standalone_build' : osx_metal.cmd_standalone_build
    },
    'linux' : {
        'not_standalone': linux.cmd_not_standalone,
        'standalone' : linux.cmd_standalone,
        'standalone_build' : linux.cmd_standalone_build
    },
    'android' : {
        'not_standalone': android.cmd_not_standalone,
        'standalone' : android.cmd_standalone,
        'standalone_build' : android.cmd_standalone_build
    },
    'iphone' : {
        'not_standalone': iphone.cmd_not_standalone,
        'standalone' : iphone.cmd_standalone,
        'standalone_build' : iphone.cmd_standalone_build
    }
    
}


def get_cmd(platform_name, api_name, test_platform_type):
    # Returns commands from platformname_apiname key if such key is present, or from platformname otherwise 
    return cmd_map.get(f'{platform_name}_{api_name}'.lower(), cmd_map.get(platform_name.lower()))[test_platform_type]
