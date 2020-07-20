const tasks = arr => arr.join(' && ')

module.exports = {
  'hooks': {
    'pre-commit': tasks([
      // 'python -m git-hook.precommit.check-file-name-extension',
      'sh ./git-hook/pre-commit'
    ])
  }
}