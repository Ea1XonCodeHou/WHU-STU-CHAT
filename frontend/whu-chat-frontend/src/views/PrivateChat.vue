<template>
  <!-- No changes to template section -->
</template>

<script setup>
import { useRoute } from 'vue-router';

const route = useRoute();
const friendId = route.params.id;
// 用 friendId 加载聊天内容
</script>
