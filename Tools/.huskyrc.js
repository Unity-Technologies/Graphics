const tasks = arr => arr.join(' && ')

module.exports = {
  'hooks': {
    'pre-commit': tasks([
      'sh ./git-hooks/pre-commit'
    ])
  }
}