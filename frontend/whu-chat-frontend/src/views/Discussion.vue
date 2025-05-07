<template>
  <div class="discussion-container">
    <!-- 讨论区左侧 - 显示讨论列表 -->
    <div class="discussion-list-container" :class="{ 'hidden-mobile': selectedDiscussion }">
      <discussion-list 
        :discussions="discussions" 
        :selectedDiscussionId="selectedDiscussion?.discussionId"
        @select-discussion="selectDiscussion" 
        @refresh-discussions="refreshDiscussions"
      />
    </div>

    <!-- 讨论区中间 - 显示所选讨论区下的帖子列表 -->
    <div class="post-list-container" v-if="selectedDiscussion" :class="{ 'hidden-mobile': selectedPostId }">
      <div class="back-button mobile-only" @click="selectedDiscussion = null">
        <i class="fas fa-arrow-left"></i> 返回讨论区
      </div>
      <post-list 
        :discussion="selectedDiscussion" 
        :posts="posts" 
        :selectedPostId="selectedPostId"
        @select-post="selectPost" 
        @refresh-posts="refreshPosts"
      />
    </div>

    <!-- 讨论区右侧 - 显示所选帖子详情 -->
    <div class="post-detail-container" v-if="selectedPostId">
      <div class="back-button mobile-only" @click="selectedPostId = null">
        <i class="fas fa-arrow-left"></i> 返回帖子列表
      </div>
      <post-detail 
        :postId="selectedPostId" 
        @close="closePostDetail" 
      />
    </div>

    <!-- 返回主页按钮 -->
    <div class="back-to-home" @click="goBack">
      <i class="fas fa-home"></i>
      <span>返回主页</span>
    </div>
  </div>
</template>

<script>
import { ref, onMounted, watch } from 'vue';
import { useRouter } from 'vue-router';
import axios from 'axios';
import DiscussionList from '@/components/DiscussionList.vue';
import PostList from '@/components/PostList.vue';
import PostDetail from '@/components/PostDetail.vue';

export default {
  name: 'DiscussionView',
  components: {
    DiscussionList,
    PostList,
    PostDetail
  },
  setup() {
    const router = useRouter();
    const discussions = ref([]);
    const selectedDiscussion = ref(null);
    const posts = ref([]);
    const selectedPostId = ref(null);

    // 获取所有讨论区
    const fetchDiscussions = async () => {
      try {
        const response = await axios.get('/api/discussion');
        discussions.value = response.data;
        
        // 如果有讨论区，默认选中第一个
        if (discussions.value.length > 0 && !selectedDiscussion.value) {
          selectDiscussion(discussions.value[0]);
        }
      } catch (error) {
        console.error('获取讨论区失败:', error);
      }
    };

    // 获取特定讨论区的帖子
    const fetchPosts = async (discussionId) => {
      try {
        const response = await axios.get(`/api/discussion/${discussionId}/posts`);
        posts.value = response.data;
      } catch (error) {
        console.error('获取帖子列表失败:', error);
      }
    };

    // 选择讨论区
    const selectDiscussion = (discussion) => {
      try {
        if (!discussion) {
          console.error('讨论区数据为空');
          return;
        }
        selectedDiscussion.value = discussion;
        // 清空选中的帖子
        selectedPostId.value = null;
        // 获取该讨论区下的帖子
        fetchPosts(discussion.discussionId);
      } catch (err) {
        console.error('选择讨论区错误:', err);
      }
    };
    
    // 选择帖子
    const selectPost = (post) => {
      try {
        if (!post || !post.postId) {
          console.error('帖子数据无效');
          return;
        }
        selectedPostId.value = post.postId;
      } catch (err) {
        console.error('选择帖子错误:', err);
      }
    };

    // 关闭帖子详情
    const closePostDetail = () => {
      selectedPostId.value = null;
      // 刷新帖子列表
      if (selectedDiscussion.value) {
        fetchPosts(selectedDiscussion.value.discussionId);
      }
    };

    // 刷新讨论区列表
    const refreshDiscussions = (newDiscussions) => {
      if (Array.isArray(newDiscussions)) {
        discussions.value = newDiscussions;
      } else {
        fetchDiscussions();
      }
    };

    // 刷新帖子列表
    const refreshPosts = (newPosts) => {
      if (Array.isArray(newPosts)) {
        posts.value = newPosts;
      } else if (selectedDiscussion.value) {
        fetchPosts(selectedDiscussion.value.discussionId);
      }
    };

    // 返回主页
    const goBack = () => {
      window.location.href = '/home';
    };

    // 监听选中的讨论区变化，加载对应的帖子
    watch(selectedDiscussion, (newVal) => {
      if (newVal) {
        fetchPosts(newVal.discussionId);
      } else {
        posts.value = [];
      }
    });

    // 组件加载时获取讨论区列表
    onMounted(() => {
      document.title = 'WHU-Chat | 讨论区';
      fetchDiscussions();
    });

    return {
      discussions,
      selectedDiscussion,
      posts,
      selectedPostId,
      selectDiscussion,
      selectPost,
      closePostDetail,
      refreshDiscussions,
      refreshPosts,
      goBack
    };
  }
};
</script>

<style scoped>
.discussion-container {
  display: flex;
  height: 100vh;
  width: 100%;
  background-color: #f8f9fa;
  font-family: 'PingFang SC', 'Microsoft YaHei', sans-serif;
  position: relative;
}

.discussion-list-container,
.post-list-container,
.post-detail-container {
  overflow: hidden;
  transition: all 0.3s ease;
}

.discussion-list-container {
  width: 25%;
  min-width: 250px;
  background-color: white;
  border-right: 1px solid #eaeaea;
}

.post-list-container {
  width: 35%;
  background-color: white;
  border-right: 1px solid #eaeaea;
}

.post-detail-container {
  flex: 1;
  background-color: white;
}

.back-button {
  display: none;
  padding: 12px 16px;
  font-weight: 500;
  color: #4776E6;
  cursor: pointer;
  background-color: #f1f5ff;
  margin-bottom: 10px;
}

.back-button i {
  margin-right: 6px;
}

.back-to-home {
  position: fixed;
  bottom: 20px;
  right: 20px;
  background: linear-gradient(135deg, #4776E6 0%, #8E54E9 100%);
  color: white;
  border-radius: 50px;
  padding: 10px 20px;
  display: flex;
  align-items: center;
  gap: 8px;
  box-shadow: 0 4px 10px rgba(71, 118, 230, 0.3);
  cursor: pointer;
  transition: transform 0.2s;
  z-index: 100;
}

.back-to-home:hover {
  transform: translateY(-3px);
}

/* 响应式设计 */
@media (max-width: 768px) {
  .discussion-container {
    display: block;
  }
  
  .discussion-list-container,
  .post-list-container,
  .post-detail-container {
    width: 100%;
    border-right: none;
    transition: all 0.3s ease;
  }
  
  .hidden-mobile {
    display: none;
  }
  
  .back-button {
    display: block;
  }
  
  .mobile-only {
    display: block;
  }
}

@media (min-width: 769px) {
  .mobile-only {
    display: none;
  }
}
</style> 