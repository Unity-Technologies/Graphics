# Following constants correspond to which log parser class to use
UTR_LOG = 'utr_log'
UNITY_LOG = 'unity_log'
EXECUTION_LOG = 'execution_log'


# tags
TAG_SUCCESFUL_RETRY = 'successful-retry' # used for instabilities that succeeded with retries
TAG_INSTABILITY = 'instability' # used for known instabilities which likely caused the job to fail
TAG_INFRASTRUCTURE = 'infrastructure' # used for infrastructure instabilities/failures
TAG_PRODUCT = 'product' # used for non-infrastructure instabilities/failures
TAG_POSSIBLE_INSTABILITY = 'possible-instability' # used for issues that are possibly instabilities, but need to be observed and verified before tagging them as such
