<template>
  <div class="post-list">
    <div class="post-list-header">
      <h2>{{ discussion.title }}</h2>
      <div class="post-list-actions">
        <button @click="refreshPosts" class="refresh-button" style="display: none;">
          <i class="fas fa-sync-alt"></i>
        </button>
        <button @click="showCreateModal = true" class="create-post-button">
          <i class="fas fa-plus"></i>
          <span>发帖</span>
        </button>
      </div>
    </div>
    
    <div class="post-list-description">
      {{ discussion.description }}
    </div>
    
    <div class="search-box">
      <i class="fas fa-search"></i>
      <input 
        type="text" 
        v-model="searchQuery" 
        placeholder="搜索帖子..." 
        @input="filterPosts"
      />
      <i 
        v-if="searchQuery" 
        class="fas fa-times-circle" 
        @click="clearSearch"
      ></i>
    </div>
    
    <div class="post-items">
      <template v-if="filteredPosts.length > 0">
        <div 
          v-for="post in filteredPosts" 
          :key="post.postId"
          class="post-item" 
          :class="{ 
            active: selectedPostId === post.postId,
            sticky: post.postType === 'sticky'
          }"
          @click="selectPost(post)"
        >
          <div class="post-meta">
            <span class="post-type" v-if="post.postType === 'sticky'">置顶</span>
            <span class="post-date">{{ formatDate(post.createTime) }}</span>
          </div>
          <h3 class="post-title">{{ post.title }}</h3>
          <div class="post-preview">{{ truncateContent(post.content) }}</div>
          <div class="post-stats">
            <div class="post-stat">
              <i class="fas fa-thumbs-up"></i>
              <span>{{ post.likeCount }}</span>
            </div>
            <div class="post-stat">
              <i class="fas fa-comment"></i>
              <span>{{ post.commentCount }}</span>
            </div>
          </div>
        </div>
      </template>
      
      <div v-else class="empty-post-list">
        <i class="fas fa-file-alt"></i>
        <p v-if="searchQuery">未找到匹配"{{ searchQuery }}"的帖子</p>
        <p v-else>暂无帖子，快来发布第一个帖子吧！</p>
        <button @click="showCreateModal = true" class="create-first-post-button">
          发布帖子
        </button>
      </div>
    </div>
    
    <!-- 创建帖子模态窗口 -->
    <div v-if="showCreateModal" class="modal-overlay" @click="showCreateModal = false">
      <div class="modal-container" @click.stop>
        <div class="modal-header">
          <h3>发布新帖子</h3>
          <button @click="showCreateModal = false" class="close-button">
            <i class="fas fa-times"></i>
          </button>
        </div>
        <div class="modal-body">
          <form @submit.prevent="createPost">
            <div class="form-group">
              <label for="title">标题</label>
              <input 
                id="title" 
                type="text" 
                v-model="newPost.title" 
                placeholder="帖子标题"
                required
              />
            </div>
            <div class="form-group">
              <label for="content">内容</label>
              <textarea 
                id="content" 
                v-model="newPost.content" 
                placeholder="帖子内容"
                required
              ></textarea>
            </div>
            <div class="form-group checkbox">
              <input 
                id="isSticky" 
                type="checkbox" 
                v-model="newPost.isSticky"
              />
              <label for="isSticky">置顶帖子</label>
            </div>
            <div class="form-group checkbox">
              <input 
                id="isAnonymous" 
                type="checkbox" 
                v-model="newPost.isAnonymous"
              />
              <label for="isAnonymous">匿名发布</label>
            </div>
            <div class="form-actions">
              <button type="button" class="cancel-button" @click="showCreateModal = false">取消</button>
              <button type="submit" class="submit-button" :disabled="isSubmitting">
                <span v-if="isSubmitting">
                  <i class="fas fa-spinner fa-spin"></i> 发布中...
                </span>
                <span v-else>发布帖子</span>
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import { ref, computed, onMounted, watch } from 'vue';
import axios from 'axios';

export default {
  name: 'PostList',
  props: {
    discussion: {
      type: Object,
      required: true
    },
    posts: {
      type: Array,
      default: () => []
    },
    selectedPostId: {
      type: Number,
      default: null
    }
  },
  emits: ['select-post', 'refresh-posts'],
  setup(props, { emit }) {
    const searchQuery = ref('');
    const filteredPosts = ref([]);
    const showCreateModal = ref(false);
    const isSubmitting = ref(false);
    const selectedPostId = ref(props.selectedPostId);
    
    // 新帖子表单数据
    const newPost = ref({
      title: '',
      content: '',
      isSticky: false,
      isAnonymous: false
    });
    
    // 筛选帖子
    const filterPosts = () => {
      if (!searchQuery.value) {
        // 默认排序：置顶的排在前面，然后按更新时间排序
        filteredPosts.value = [...props.posts].sort((a, b) => {
          if (a.postType === 'sticky' && b.postType !== 'sticky') return -1;
          if (a.postType !== 'sticky' && b.postType === 'sticky') return 1;
          return new Date(b.updateTime) - new Date(a.updateTime);
        });
        return;
      }
      
      const query = searchQuery.value.toLowerCase();
      filteredPosts.value = props.posts.filter(post => 
        post.title.toLowerCase().includes(query) || 
        post.content.toLowerCase().includes(query)
      ).sort((a, b) => {
        if (a.postType === 'sticky' && b.postType !== 'sticky') return -1;
        if (a.postType !== 'sticky' && b.postType === 'sticky') return 1;
        return new Date(b.updateTime) - new Date(a.updateTime);
      });
    };
    
    // 清除搜索
    const clearSearch = () => {
      searchQuery.value = '';
      filterPosts();
    };
    
    // 截断帖子内容
    const truncateContent = (content) => {
      if (!content) return '';
      if (content.length <= 100) return content;
      return content.substring(0, 100) + '...';
    };
    
    // 格式化日期
    const formatDate = (dateStr) => {
      if (!dateStr) return '';
      const date = new Date(dateStr);
      const now = new Date();
      
      const diffMs = now - date;
      const diffMins = Math.floor(diffMs / (1000 * 60));
      const diffHours = Math.floor(diffMs / (1000 * 60 * 60));
      const diffDays = Math.floor(diffMs / (1000 * 60 * 60 * 24));
      
      if (diffMins < 1) return '刚刚';
      if (diffMins < 60) return `${diffMins}分钟前`;
      if (diffHours < 24) return `${diffHours}小时前`;
      if (diffDays < 30) return `${diffDays}天前`;
      
      return date.toLocaleDateString();
    };
    
    // 选择帖子
    const selectPost = (post) => {
      try {
        if (!post || !post.postId) {
          console.error('帖子数据无效');
          return;
        }
        selectedPostId.value = post.postId;
        emit('select-post', {...post});
      } catch (err) {
        console.error('选择帖子错误:', err);
      }
    };
    
    // 创建新帖子
    const createPost = async () => {
      if (!newPost.value.title || !newPost.value.content) {
        alert('请填写完整的帖子信息');
        return;
      }
      
      isSubmitting.value = true;
      try {
        const userId = localStorage.getItem('userId');
        if (!userId) {
          alert('请先登录');
          return;
        }
        
        const postData = {
          discussionId: props.discussion.discussionId,
          title: newPost.value.title,
          content: newPost.value.content,
          authorId: parseInt(userId),
          postType: newPost.value.isSticky ? 'sticky' : 'normal',
          isAnonymous: newPost.value.isAnonymous
        };
        
        const response = await axios.post(`/api/discussion/${props.discussion.discussionId}/posts`, postData);
        console.log('创建帖子成功:', response.data);
        
        // 重置表单并关闭模态窗口
        newPost.value = {
          title: '',
          content: '',
          isSticky: false,
          isAnonymous: false
        };
        showCreateModal.value = false;
        
        // 重新获取帖子列表
        refreshPosts();
      } catch (error) {
        console.error('创建帖子失败:', error);
        alert('创建帖子失败: ' + (error.response?.data || error.message));
      } finally {
        isSubmitting.value = false;
      }
    };
    
    // 刷新帖子列表
    const refreshPosts = async () => {
      try {
        if (!props.discussion || !props.discussion.discussionId) return;
        
        const response = await axios.get(`/api/discussion/${props.discussion.discussionId}/posts`);
        // 直接发射事件通知父组件更新帖子列表
        emit('refresh-posts', response.data);
      } catch (error) {
        console.error('刷新帖子列表失败:', error);
      }
    };
    
    // 监听帖子列表变化
    watch(() => props.posts, (newVal) => {
      if (newVal) {
        filterPosts();
      }
    }, { immediate: true, deep: true });
    
    // 初始化
    onMounted(() => {
      filterPosts();
    });
    
    return {
      searchQuery,
      showCreateModal,
      isSubmitting,
      newPost,
      filteredPosts,
      selectedPostId,
      filterPosts,
      clearSearch,
      truncateContent,
      formatDate,
      selectPost,
      createPost,
      refreshPosts
    };
  }
};
</script>

<style scoped>
.post-list {
  display: flex;
  flex-direction: column;
  height: 100%;
  background-color: white;
}

.post-list-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 20px;
  border-bottom: 1px solid #eaeaea;
}

.post-list-header h2 {
  font-size: 20px;
  color: #333;
  margin: 0;
  font-weight: 600;
}

.post-list-actions {
  display: flex;
  gap: 10px;
}

.refresh-button {
  width: 36px;
  height: 36px;
  border-radius: 18px;
  background-color: #f1f5ff;
  color: #4776E6;
  border: none;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  transition: all 0.2s;
}

.refresh-button:hover {
  transform: scale(1.1);
  background-color: #e6eeff;
}

.refresh-button i {
  font-size: 14px;
}

.create-post-button {
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, #4776E6 0%, #8E54E9 100%);
  color: white;
  border: none;
  padding: 8px 16px;
  border-radius: 4px;
  font-size: 14px;
  cursor: pointer;
  transition: all 0.2s ease;
}

.create-post-button i {
  margin-right: 6px;
}

.create-post-button:hover {
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(71, 118, 230, 0.3);
}

.post-list-description {
  padding: 0 20px 20px;
  color: #666;
  font-size: 14px;
  line-height: 1.5;
  border-bottom: 1px solid #eaeaea;
}

.search-box {
  display: flex;
  align-items: center;
  padding: 10px 20px;
  background-color: #f8f9fa;
  position: relative;
}

.search-box i {
  color: #999;
  margin-right: 8px;
}

.search-box input {
  flex: 1;
  border: none;
  background: transparent;
  outline: none;
  font-size: 14px;
  padding: 8px 0;
}

.search-box .fa-times-circle {
  cursor: pointer;
  color: #999;
  margin-left: 8px;
  margin-right: 0;
}

.post-items {
  flex: 1;
  overflow-y: auto;
  padding: 15px;
}

.post-item {
  background-color: white;
  border-radius: 8px;
  padding: 15px;
  margin-bottom: 15px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.05);
  cursor: pointer;
  transition: all 0.2s ease;
  position: relative;
}

.post-item:hover {
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
}

.post-item.active {
  border-left: 4px solid #4776E6;
  background-color: #f9fbff;
}

.post-item.sticky {
  background-color: #fff9e6;
  border: 1px solid #ffecb3;
}

.post-meta {
  display: flex;
  justify-content: space-between;
  margin-bottom: 8px;
  font-size: 12px;
}

.post-type {
  background-color: #ff9800;
  color: white;
  padding: 2px 6px;
  border-radius: 3px;
  font-weight: 500;
}

.post-date {
  color: #999;
}

.post-title {
  margin: 0 0 10px;
  font-size: 16px;
  color: #333;
  font-weight: 600;
  line-height: 1.4;
}

.post-preview {
  font-size: 14px;
  color: #666;
  margin-bottom: 12px;
  line-height: 1.5;
  max-height: 65px;
  overflow: hidden;
  text-overflow: ellipsis;
}

.post-stats {
  display: flex;
  gap: 15px;
}

.post-stat {
  display: flex;
  align-items: center;
  font-size: 12px;
  color: #999;
}

.post-stat i {
  margin-right: 5px;
}

.empty-post-list {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 50px 20px;
  text-align: center;
}

.empty-post-list i {
  font-size: 50px;
  color: #ddd;
  margin-bottom: 15px;
}

.empty-post-list p {
  color: #999;
  margin-bottom: 20px;
}

.create-first-post-button {
  background: linear-gradient(135deg, #4776E6 0%, #8E54E9 100%);
  color: white;
  border: none;
  padding: 8px 16px;
  border-radius: 4px;
  font-size: 14px;
  cursor: pointer;
  transition: all 0.2s;
}

.create-first-post-button:hover {
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(71, 118, 230, 0.3);
}

/* 模态窗口样式 */
.modal-overlay {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background-color: rgba(0, 0, 0, 0.5);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
}

.modal-container {
  width: 90%;
  max-width: 600px;
  background-color: white;
  border-radius: 8px;
  box-shadow: 0 5px 20px rgba(0, 0, 0, 0.2);
  overflow: hidden;
  animation: modalFadeIn 0.3s ease;
}

@keyframes modalFadeIn {
  from {
    opacity: 0;
    transform: translateY(-20px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

.modal-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 15px 20px;
  border-bottom: 1px solid #eaeaea;
}

.modal-header h3 {
  margin: 0;
  color: #333;
  font-size: 18px;
}

.close-button {
  background: none;
  border: none;
  font-size: 16px;
  color: #666;
  cursor: pointer;
}

.modal-body {
  padding: 20px;
}

.form-group {
  margin-bottom: 15px;
}

.form-group label {
  display: block;
  margin-bottom: 5px;
  font-weight: 500;
  color: #555;
}

.form-group input[type="text"],
.form-group textarea {
  width: 100%;
  padding: 10px;
  border: 1px solid #ddd;
  border-radius: 4px;
  font-size: 14px;
  transition: border-color 0.2s;
}

.form-group input[type="text"]:focus,
.form-group textarea:focus {
  border-color: #4776E6;
  outline: none;
}

.form-group textarea {
  height: 200px;
  resize: vertical;
}

.form-group.checkbox {
  display: flex;
  align-items: center;
}

.form-group.checkbox input {
  margin-right: 8px;
}

.form-actions {
  display: flex;
  justify-content: flex-end;
  gap: 10px;
  margin-top: 20px;
}

.cancel-button,
.submit-button {
  padding: 8px 16px;
  border: none;
  border-radius: 4px;
  font-size: 14px;
  cursor: pointer;
  transition: all 0.2s;
}

.cancel-button {
  background-color: #f0f0f0;
  color: #333;
}

.submit-button {
  background: linear-gradient(135deg, #4776E6 0%, #8E54E9 100%);
  color: white;
}

.cancel-button:hover {
  background-color: #e0e0e0;
}

.submit-button:hover {
  box-shadow: 0 5px 15px rgba(71, 118, 230, 0.3);
}

.submit-button:disabled {
  opacity: 0.7;
  cursor: not-allowed;
}
</style> 