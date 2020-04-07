from . import win, osx_openglcore, osx_metal, linux

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
    }
    
}


def get_cmd(platform_name, api_name, test_platform_type):
    '''Returns the set of commands according to the platform, api, and job (editmode: normal editmode; editmode_split: editmode with split standalone_build for standalone; standalone_build: standalone_build player for standalone)'''
    cmd_map_ref = f'{platform_name}_{api_name}'.lower() if platform_name.lower() == 'osx' else platform_name.lower()
    return cmd_map[cmd_map_ref][test_platform_type]
