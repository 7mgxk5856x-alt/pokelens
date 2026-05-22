import js from '@eslint/js';
import prettierConfig from 'eslint-config-prettier';
import globals from 'globals';

export default [
  js.configs.recommended,
  prettierConfig,
  {
    languageOptions: {
      globals: {
        ...globals.browser,
      },
    },
    rules: {
      'no-unused-vars': ['error', { argsIgnorePattern: '^_' }],
      'no-console': 'warn',
      // var は使わず const 既定・必要時 let（development-guidelines「変数宣言」）
      'no-var': 'error',
      'prefer-const': 'error',
      // 制御構文の本体は一行でも必ず波括弧で囲む（development-guidelines「制御構文のブロック」）
      curly: ['error', 'all'],
    },
  },
  {
    files: ['tests/**/*.js'],
    languageOptions: {
      globals: {
        ...globals.node,
      },
    },
  },
  {
    ignores: ['node_modules/', 'dist/', 'coverage/'],
  },
];
