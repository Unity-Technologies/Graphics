from . import win, osx_openglcore, osx_metal, linux

cmd_map = {
    'win' : {
        'test' : win.cmd_test,
        'test_standalone' : win.cmd_test_standalone,
        'build' : win.cmd_build
    },
    'osx_openglcore' :  {
        'test' : osx_openglcore.cmd_test,
        'test_standalone' : osx_openglcore.cmd_test_standalone,
        'build' : osx_openglcore.cmd_build
    },
    'osx_metal' :  {
        'test' : osx_metal.cmd_test,
        'test_standalone' : osx_metal.cmd_test_standalone,
        'build' : osx_metal.cmd_build
    },
    'linux' : {
        'test' : linux.cmd_test,
        'test_standalone' : linux.cmd_test_standalone,
        'build' : linux.cmd_build
    }
    
}


def get_cmd(platform_name, api_name, job_type):
    '''Returns the set of commands according to the platform, api, and job (test: normal test; test_split: test with split build for standalone; build: build player for standalone)'''
    cmd_map_ref = f'{platform_name}_{api_name}'.lower() if platform_name.lower() == 'osx' else platform_name.lower()
    return cmd_map[cmd_map_ref][job_type]
