import abc

class Job(abc.ABC):
    __metaclass__ = abc.ABCMeta

    @abc.abstractmethod
    def get_job_id(self):
        pass

    @abc.abstractmethod
    def get_job_path(self):
        pass

    @abc.abstractmethod
    def get_yml_job(self, new_name):
        pass
    