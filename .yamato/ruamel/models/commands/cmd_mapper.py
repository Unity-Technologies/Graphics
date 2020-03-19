from . import win, osx_openglcore, osx_metal, linux

cmd_map = {
    'win' : {
        'editmode' : win.cmd_editmode,
        'playmode' : win.cmd_playmode,
        'standalone' : win.cmd_standalone,
        'standalone_build' : win.cmd_standalone_build
    },
    'osx_openglcore' :  {
        'editmode' : osx_openglcore.cmd_editmode,
        'playmode' : osx_openglcore.cmd_playmode,
        'standalone' : osx_openglcore.cmd_standalone,
        'standalone_build' : osx_openglcore.cmd_standalone_build
    },
    'osx_metal' :  {
        'editmode' : osx_metal.cmd_editmode,
        'playmode' : osx_metal.cmd_playmode,
        'standalone' : osx_metal.cmd_standalone,
        'standalone_build' : osx_metal.cmd_standalone_build
    },
    'linux' : {
        'editmode' : linux.cmd_editmode,
        'playmode' : linux.cmd_playmode,
        'standalone' : linux.cmd_standalone,
        'standalone_build' : linux.cmd_standalone_build
    }
    
}


def get_cmd(platform_name, api_name, test_platform_name):
    '''Returns the set of commands according to the platform, api, and job (editmode: normal editmode; editmode_split: editmode with split standalone_build for standalone; standalone_build: standalone_build player for standalone)'''
    cmd_map_ref = f'{platform_name}_{api_name}'.lower() if platform_name.lower() == 'osx' else platform_name.lower()
    return cmd_map[cmd_map_ref][test_platform_name]
